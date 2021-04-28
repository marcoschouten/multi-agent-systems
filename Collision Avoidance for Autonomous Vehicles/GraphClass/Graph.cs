using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    TerrainManager terrain_manager;
    float firingRange;
    float carLength;
    int num_cars;
    float diff_group_penalty = 200.0f;      // penalty for edges of vertices from different groups, timed with the distance as the final weight
    Vector3 center_vertex;  // the closest vertex to the starting position of the car, ideally where the spanning tree starts
    int center_vertex_idx;

    private Hashtable vertices; // idx - Vector3
    private Hashtable edges;    // idx - idx
    private Hashtable groups;   // grouping the vertices into groups of the same number of cars
    Vector3 division_vec = new Vector3(0.0f, 0.0f, 1.0f);


    public Graph(TerrainManager terrain_manager, float firingRange, float carLength, int num_cars, Vector3 init_car_pos)
    {
        this.terrain_manager = terrain_manager;
        this.firingRange = firingRange;
        this.carLength = carLength;
        this.num_cars = num_cars;

        (this.vertices, this.edges) = createGraph();
        (this.center_vertex, this.center_vertex_idx) = Nearest(vertices, init_car_pos);
        // debug marco
        this.center_vertex += new Vector3(0.1f, 0.0f, 0.1f);
        // debug marco
        groupVertices();
    }

    public Hashtable getVertices()
    {
        return this.vertices;
    }

    public Hashtable getEdges()
    {
        return this.edges;
    }

    public List<int> getEdges(int idx)
    {
        return (List<int>)this.edges[idx];
    }

    public float getWeight(int i, int j)
    {
        // return weights of edge i-j, given indices of two vertices


        if (groups[i] == groups[j] || (i == this.center_vertex_idx || j == this.center_vertex_idx))
        {
            return Vector3.Distance((Vector3)vertices[i], (Vector3)vertices[j]);
        }
        else
        {
            return Vector3.Distance((Vector3)vertices[i], (Vector3)vertices[j]) * diff_group_penalty;
        }

        //return Vector3.Distance((Vector3)vertices[i], (Vector3)vertices[j]);
    }

    public (Vector3, int) getRootVertex()
    {
        return (this.center_vertex, this.center_vertex_idx);
    }

    public int getGroup(int v_idx)
    {
        return (int)groups[v_idx];
    }

    private (Hashtable, Hashtable) createGraph()
    {

        // create Hashtables of vertices (idx - position) and edges (idx - list of idx)

        List<Vector3> vertices_pos = getVerticesPos();
        float min_dist = Vector3.Distance(vertices_pos[0], vertices_pos[1]);
        for (int i=2; i<vertices_pos.Count; i++)
        {
            float dist = Vector3.Distance(vertices_pos[0], vertices_pos[i]);
            if (dist < min_dist)
            {
                min_dist = dist;
            }
        }

        Hashtable vertices = new Hashtable();
        Hashtable edges = new Hashtable();
        for (int i = 0; i < vertices_pos.Count; i++)
        {
            vertices.Add(i, vertices_pos[i]);

            List<int> i_edges = new List<int>();
            for (int j = 0; j < vertices_pos.Count; j++)
            {
                if (j != i && CollisionFree(vertices_pos[i], vertices_pos[j]))
                {
                    //i_edges.Add(j);
                    if (Vector3.Distance(vertices_pos[i], vertices_pos[j]) < 1.4f * min_dist)
                    {
                        i_edges.Add(j);
                    }

                }
            }
            edges.Add(i, i_edges);
        }

        return (vertices, edges);
    }

    private List<Vector3> getVerticesPos()
    {

        // get the positions of the vertices based on the firing range


        // full coverage
        //float r = this.firingRange / (float)Math.Pow(2.0f, 0.5f);
        //// almost full coverage, might be enough
        float r = this.firingRange;

        (float x_low, float z_low, float x_high, float z_high) = xzBoundary();

        List<float> x_list = new List<float>();
        List<float> z_list = new List<float>();
        int x_split = (int)((x_high - x_low) / (2 * r));
        int z_split = (int)((z_high - z_low) / (2 * r));
        for (int i = 0; i < x_split; i++)
        {
            x_list.Add(r + 2 * i * r + x_low);
        }
        for (int j = 0; j < z_split; j++)
        {
            z_list.Add(r + 2 * j * r + z_low);
        }

        int[,] vertexMatrix = new int[x_split, z_split];
        List<Vector3> vertices_pos = new List<Vector3>();
        for (int i = 0; i < x_list.Count; i++)
        {
            for (int j = 0; j < z_list.Count; j++)
            {
                float x = x_list[i];
                float z = z_list[j];

                if (traversable(x, z))
                {
                    vertexMatrix[i, j] = 1;
                    while (!traversable(x, z + this.carLength))
                    {
                        z = z - 1.0f;
                    }
                    while (!traversable(x, z - this.carLength))
                    {
                        z = z + 1.0f;
                    }
                    while (!traversable(x + this.carLength, z))
                    {
                        x = x - 1.0f;
                    }
                    while (!traversable(x - this.carLength, z))
                    {
                        x = x + 1.0f;
                    }
                    vertices_pos.Add(new Vector3(x, 0.0f, z));
                }
                else
                {
                    vertexMatrix[i, j] = 0;
                }
            }
        }


        return vertices_pos;
    }

    private (List<Vector3>, int) RefineVerticesPos(List<float> x_list, List<float> z_list, int[,] vertexMatrix, List<Vector3> vertices_pos)
    {
        List<List<float>> boxCorners = new List<List<float>>();
        for (int i=0; i<vertexMatrix.GetLength(0) - 1; i++)
        {
            for (int j=0; j<vertexMatrix.GetLength(1) - 1; j++)
            {
                bool notConsidered = !InBox(new Vector3(x_list[i] + 0.1f, 0.0f, z_list[j] + 0.1f), boxCorners) && !InBox(new Vector3(x_list[i] + 0.1f, 0.0f, z_list[j] - 0.1f), boxCorners) && !InBox(new Vector3(x_list[i] - 0.1f, 0.0f, z_list[j] - 0.1f), boxCorners) && !InBox(new Vector3(x_list[i] + 0.1f, 0.0f, z_list[j] + 0.1f), boxCorners);
                if (notConsidered && AllOneBox(vertexMatrix, i, i+1, j, j + 1))
                {
                    if (i == vertexMatrix.GetLength(0) - 2)
                    {
                        if (j == vertexMatrix.GetLength(1) - 2)
                        {
                            List<float> corners = new List<float>() { x_list[i], x_list[i + 1], z_list[j], z_list[j + 1] };
                            boxCorners.Add(corners);
                        }
                        else
                        {
                            for (int bottom = j + 2; bottom < vertexMatrix.GetLength(1); bottom++)
                            {
                                if (!AllOneBox(vertexMatrix, i, i+1, j, bottom))
                                {
                                    List<float> corners = new List<float>() { x_list[i], x_list[i + 1], z_list[j], z_list[bottom - 1] };
                                    boxCorners.Add(corners);
                                    break;
                                }
                            }
                        }
                    } 
                    else if (j == vertexMatrix.GetLength(1) - 2)
                    {
                        for (int right = i + 2; right < vertexMatrix.GetLength(0); right++)
                        {
                            if (!AllOneBox(vertexMatrix, i, right, j, j + 1))
                            {
                                List<float> corners = new List<float>() { x_list[i], x_list[right - 1], z_list[j], z_list[j + 1] };
                                boxCorners.Add(corners);
                                break;
                            }
                        }
                    }
                    else
                    {
                        int bottom = j + 2;
                        int right = i + 2;
                        for (int test_right = right; test_right < vertexMatrix.GetLength(0); test_right++)
                        {
                            if (AllOneBox(vertexMatrix, i, test_right, j, bottom))
                            {
                                bottom++;
                            }
                            else
                            {
                                bottom--;
                                right = test_right - 1;
                            }
                        }
                        if (!AllOneBox(vertexMatrix, i, right+1, j, bottom))
                        {
                            for (int test_bottom = bottom + 1; test_bottom < vertexMatrix.GetLength(1); test_bottom++)
                            {
                                if (!AllOneBox(vertexMatrix, i, right, j, test_bottom))
                                {
                                    List<float> corners = new List<float>() { x_list[i], x_list[right], z_list[j], z_list[test_bottom - 1] };
                                    boxCorners.Add(corners);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int test_right = right + 1; test_right < vertexMatrix.GetLength(0); test_right++)
                            {
                                if (!AllOneBox(vertexMatrix, i, test_right, j, bottom))
                                {
                                    List<float> corners = new List<float>() { x_list[i], x_list[test_right - 1], z_list[j], z_list[bottom] };
                                    boxCorners.Add(corners);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        foreach (List<float> corner in boxCorners)
        {
            //Debug.Log(corner[0].ToString() + " " + corner[1].ToString() + " " + corner[2].ToString() + " " + corner[3].ToString());
            Debug.DrawLine(new Vector3(corner[0], 0.0f, corner[2]), new Vector3(corner[1], 0.0f, corner[2]), Color.cyan, 1000f);
            Debug.DrawLine(new Vector3(corner[0], 0.0f, corner[3]), new Vector3(corner[1], 0.0f, corner[3]), Color.cyan, 1000f);
            Debug.DrawLine(new Vector3(corner[0], 0.0f, corner[2]), new Vector3(corner[0], 0.0f, corner[3]), Color.cyan, 1000f);
            Debug.DrawLine(new Vector3(corner[1], 0.0f, corner[2]), new Vector3(corner[1], 0.0f, corner[3]), Color.cyan, 1000f);
        }

        return (vertices_pos, 0);

        //if (boxCorners.Count == 0)
        //{
        //    return (vertices_pos, 0);
        //}
        //else
        //{
        //    List<List<float>> maxBoxCorners = new List<List<float>>();
        //    foreach (List<float> corners in boxCorners)
        //    {
        //        maxBoxCorners.Add(ExpandCorners(corners));
        //    }

        //    (List<Vector3> newPos, int newVertexCount) = GetVerticesFromBox(maxBoxCorners, 4 * firingRange);
        //    foreach (Vector3 vertex in vertices_pos)
        //    {
        //        if (!InBox(vertex, maxBoxCorners))
        //        {
        //            newPos.Add(vertex);
        //        }
        //    }


        //    return (newPos, newVertexCount);

        //}
    }

    private (List<Vector3> newPos, int newVertexCount) GetVerticesFromBox(List<List<float>> maxBoxCorners, float range)
    {
        List<Vector3> newPos = new List<Vector3>();
        foreach (List<float> corners in maxBoxCorners)
        {
            for (float x = corners[0] + range / 2.0f; x < corners[1]; x += range)
            {
                for (float z = corners[2] + range / 2.0f; z < corners[3]; z += range)
                {
                    if (traversable(x, z))
                    {
                        newPos.Add(new Vector3(x, 0.0f, z));
                    }
                }
            }
        }

        return (newPos, newPos.Count);
    }

    private bool InBox(Vector3 vertex, List<List<float>> maxBoxCorners)
    {
        bool inbox = false;
        foreach (List<float> corners in maxBoxCorners)
        {
            if (vertex.x > corners[0] && vertex.x < corners[1] && vertex.z > corners[2] && vertex.z < corners[3])
            {
                inbox = true;
                break;
            }
        }

        return inbox;
    }

    private List<float> ExpandCorners(List<float> corners)
    {
        (float x_low, float x_high, float z_low, float z_high) = xzBoundary();

        for (float left=corners[0] - 1.0f; left>Math.Max(x_low, corners[0] - 2 * firingRange); left -= 1.0f)
        {
            if (!CollisionFree(new Vector3(left, 0.0f, corners[2]), new Vector3(left, 0.0f, corners[3])))
            {
                corners[0] = left + 1.0f;
            }
        }

        for (float right = corners[1] + 1.0f; right < Math.Min(x_high, corners[1] + 2 * firingRange); right += 1.0f)
        {
            if (!CollisionFree(new Vector3(right, 0.0f, corners[2]), new Vector3(right, 0.0f, corners[3])))
            {
                corners[1] = right - 1.0f;
            }
        }

        for (float top = corners[2] - 1.0f; top > Math.Max(z_low, corners[2] - 2 * firingRange); top -= 1.0f)
        {
            if (!CollisionFree(new Vector3(corners[0], 0.0f, top), new Vector3(corners[1], 0.0f, top)))
            {
                corners[2] = top + 1.0f;
            }
        }

        for (float bottom = corners[3] + 1.0f; bottom < Math.Max(z_high, corners[3] + 2 * firingRange); bottom += 1.0f)
        {
            if (!CollisionFree(new Vector3(corners[0], 0.0f, bottom), new Vector3(corners[1], 0.0f, bottom)))
            {
                corners[3] = bottom - 1.0f;
            }
        }

        return corners;
    }

    private bool AllOneBox(int[,] matrix, int left, int right, int top, int bottom)
    {
        if (left < 0 || top < 0 || right >= matrix.GetLength(0) || bottom >= matrix.GetLength(1))
        {
            return false;
        }
        bool allOne = true;
        for (int i=left; i< right + 1; i++)
        {
            for (int j=top; j<bottom + 1; j++)
            {
                if (matrix[i, j] == 0)
                {
                    allOne = false;
                    break;
                }
            }
        }

        return allOne;
    }

    private void groupVertices()
    {
        Vector3 center_vertex = this.center_vertex;
        int group_member_count = vertices.Count / num_cars;
        

        // divide the vertices into equal angle groups first
        Hashtable angle_groups = new Hashtable();

        int angle_split = num_cars * 10;    // higher number has higher precision, but more computationally costly
        for (int i=0; i<angle_split + 1; i++)
        {
            angle_groups.Add(i, new List<int>());
        }

        float angle_delta = 360 / angle_split;

        foreach (int vertex_idx in vertices.Keys)
        {
            Vector3 vertex = (Vector3)vertices[vertex_idx];
            float angle = SignedAngleBetween(vertex - center_vertex, division_vec, new Vector3(0.0f, 1.0f, 0.0f));
            int angle_group_idx = (int)((angle + 180) / angle_delta);
            List<int> i_group = (List<int>)(angle_groups[angle_group_idx]);
            i_group.Add(vertex_idx);
            angle_groups[angle_group_idx] = i_group;
        }


        // combine angle groups to form final groups with approximately equal number of vertices
        int count = 0;
        int group_idx = 0;
        groups = new Hashtable();
        for (int i=0; i < angle_split + 1; i++)
        {
            List<int> i_group = (List<int>)(angle_groups[i]);
            count += i_group.Count;
            if (count > (group_idx + 1) * group_member_count)
            {
                group_idx += 1;
            }
            if (count <= (group_idx + 1) * group_member_count)
            {
                foreach (int vertex_idx in i_group)
                {
                    groups.Add(vertex_idx, group_idx);
                }
            }
        }
    }

    public (Vector3, int) Nearest(Hashtable Vertices, Vector3 x_given)
    {
        Vector3 x_nearest = new Vector3();
        int x_nearest_key = -1;
        float min_dist = 1000f;
        foreach (int key in Vertices.Keys)
        {
            Vector3 x = (Vector3)Vertices[key];
            float dist = Vector3.Distance(x, x_given);

            if (dist < min_dist)
            {
                x_nearest = x;
                min_dist = dist;
                x_nearest_key = key;
            }
        }
        return (x_nearest, x_nearest_key);
    }

    private bool traversable(float x_pos, float z_pos)
    {
        int ind_i = terrain_manager.myInfo.get_i_index(x_pos);
        int ind_j = terrain_manager.myInfo.get_j_index(z_pos);
        float transversibility = terrain_manager.myInfo.traversability[ind_i, ind_j];
        if (transversibility == 0.0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool CollisionFree(Vector3 pos1, Vector3 pos2)
    {
        int testing_points = (int)Vector3.Distance(pos1, pos2) * 5;
        bool obstFree = true;

        for (int i = 1; i < testing_points; i++)
        {
            Vector3 pos_new = (pos2 - pos1) * i / (testing_points - 1) + pos1;
            if (!traversable(pos_new.x, pos_new.z))
            {
                obstFree = false;
                break;
            }
        }
        return obstFree;
    }


    private float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        return signed_angle;
    }

    private (float, float, float, float) xzBoundary()
    {
        float x_low = terrain_manager.myInfo.x_low;
        float x_high = terrain_manager.myInfo.x_high;
        float z_low = terrain_manager.myInfo.z_low;
        float z_high = terrain_manager.myInfo.z_high;
        float x_boundary_low = x_low;
        float z_boundary_low = z_low;
        float x_boundary_high = x_high;
        float z_boundary_high = z_high;

        // x low
        for (float i=1.0f; i<x_high - x_low; i+=1.0f)
        {
            bool boundary = true;
            for (float j= 0.0f; j < z_high - z_low; j += 1.0f)
            {
                if (traversable(i + x_low, j + z_low))
                {
                    boundary = false;
                    break;
                }
            }

            if (boundary == false)
            {
                x_boundary_low = i + x_low;
                break;
            }
        }

        // z low
        for (float i = 1.0f; i < z_high - z_low; i += 1.0f)
        {
            bool boundary = true;
            for (float j = 0.0f; j < x_high - x_low; j += 1.0f)
            {
                if (traversable(j + x_low, i + z_low))
                {
                    boundary = false;
                    break;
                }
            }

            if (boundary == false)
            {
                z_boundary_low = i + z_low;
                break;
            }
        }

        // x high
        for (float i = - 1.0f; i > x_low - x_high; i -= 1.0f)
        {
            bool boundary = true;
            for (float j = -1.0f; j > z_low - z_high; j -= 1.0f)
            {
                if (traversable(i + x_high, j + z_high))
                {
                    boundary = false;
                    break;
                }
            }

            if (boundary == false)
            {
                x_boundary_high = i + x_high;
                break;
            }
        }

        // z high
        for (float i = -1.0f; i > z_low - z_high; i -= 1.0f)
        {
            bool boundary = true;
            for (float j = -1.0f; j > x_low - x_high; j -= 1.0f)
            {
                if (traversable(j + x_high, i + z_high))
                {
                    boundary = false;
                    break;
                }
            }

            if (boundary == false)
            {
                z_boundary_high = i + z_high;
                break;
            }
        }

        return (x_boundary_low - 1.0f, z_boundary_low - 1.0f, x_boundary_high + 1.0f, z_boundary_high + 1.0f);
    }

}
