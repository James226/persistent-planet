using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using PersistentPlanet.Graphics;

namespace PersistentPlanet.DualContouring
{
    public class Octree
    {
        public static int MATERIAL_AIR = 0;
        public static int MATERIAL_SOLID = 1;

        public static float QEF_ERROR = 1e-6f;
        public static int QEF_SWEEPS = 4;

        private static readonly Queue<OctreeNode> _spareNodes = new Queue<OctreeNode>();
        private static readonly Queue<OctreeDrawInfo> _spareDrawInfo = new Queue<OctreeDrawInfo>();

        public static readonly Vector3[] CHILD_MIN_OFFSETS =
        {
            // needs to match the vertMap from Dual Contouring impl
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
        };

        // data from the original DC impl, drives the contouring process

        private static readonly int[][] Edgevmap =
        {
            new[] {2, 4},
            new[] {1, 5},
            new[] {2, 6},
            new[] {3, 7}, // x-axis 
            new[] {0, 2},
            new[] {1, 3},
            new[] {4, 6},
            new[] {5, 7}, // y-axis
            new[] {0, 1},
            new[] {2, 3},
            new[] {4, 5},
            new[] {6, 7} // z-axis
        };

        public static readonly int[] edgemask = {5, 3, 6};

        public static readonly int[][] vertMap =
        {
            new[] {0, 0, 0},
            new[] {0, 0, 1},
            new[] {0, 1, 0},
            new[] {0, 1, 1},
            new[] {1, 0, 0},
            new[] {1, 0, 1},
            new[] {1, 1, 0},
            new[] {1, 1, 1}
        };

        public static readonly int[][] faceMap =
        {
            new[] {4, 8, 5, 9},
            new[] {6, 10, 7, 11},
            new[] {0, 8, 1, 10},
            new[] {2, 9, 3, 11},
            new[] {0, 4, 2, 6},
            new[] {1, 5, 3, 7}
        };

        public static readonly int[][] cellProcFaceMask =
        {
            new[] {0, 4, 0},
            new[] {1, 5, 0},
            new[] {2, 6, 0},
            new[] {3, 7, 0},
            new[] {0, 2, 1},
            new[] {4, 6, 1},
            new[] {1, 3, 1},
            new[] {5, 7, 1},
            new[] {0, 1, 2},
            new[] {2, 3, 2},
            new[] {4, 5, 2},
            new[] {6, 7, 2}
        };

        public static readonly int[][] cellProcEdgeMask =
        {
            new[] {0, 1, 2, 3, 0},
            new[] {4, 5, 6, 7, 0},
            new[] {0, 4, 1, 5, 1},
            new[] {2, 6, 3, 7, 1},
            new[] {0, 2, 4, 6, 2},
            new[] {1, 3, 5, 7, 2}
        };

        public static readonly int[][][] faceProcFaceMask =
        {
            new[] {new[] {4, 0, 0}, new[] {5, 1, 0}, new[] {6, 2, 0}, new[] {7, 3, 0}},
            new[] {new[] {2, 0, 1}, new[] {6, 4, 1}, new[] {3, 1, 1}, new[] {7, 5, 1}},
            new[] {new[] {1, 0, 2}, new[] {3, 2, 2}, new[] {5, 4, 2}, new[] {7, 6, 2}}
        };

        public static readonly int[][][] faceProcEdgeMask =
        {
            new[] {new[] {1, 4, 0, 5, 1, 1}, new[] {1, 6, 2, 7, 3, 1}, new[] {0, 4, 6, 0, 2, 2}, new[] {0, 5, 7, 1, 3, 2}},
            new[] {new[] {0, 2, 3, 0, 1, 0}, new[] {0, 6, 7, 4, 5, 0}, new[] {1, 2, 0, 6, 4, 2}, new[] {1, 3, 1, 7, 5, 2}},
            new[] {new[] {1, 1, 0, 3, 2, 0}, new[] {1, 5, 4, 7, 6, 0}, new[] {0, 1, 5, 0, 4, 1}, new[] {0, 3, 7, 2, 6, 1}}
        };

        public static readonly int[][][] edgeProcEdgeMask =
        {
            new[] {new[] {3, 2, 1, 0, 0}, new[] {7, 6, 5, 4, 0}},
            new[] {new[] {5, 1, 4, 0, 1}, new[] {7, 3, 6, 2, 1}},
            new[] {new[] {6, 4, 2, 0, 2}, new[] {7, 5, 3, 1, 2}},
        };

        public static readonly int[][] processEdgeMask =
        {
            new[] {3, 2, 1, 0},
            new[] {7, 5, 6, 4},
            new[] {11, 10, 9, 8}
        };


        public static OctreeNode SimplifyOctree(OctreeNode node, float threshold)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Type != OctreeNodeType.Node_Internal)
            {
                // can't simplify!
                return node;
            }

            var qef = new QefSolver();
            var signs = new[] {-1, -1, -1, -1, -1, -1, -1, -1};
            var midsign = -1;
            var isCollapsible = true;

            for (var i = 0; i < 8; i++)
            {
                node.children[i] = SimplifyOctree(node.children[i], threshold);

                if (node.children[i] != null)
                {
                    var child = node.children[i];

                    if (child.Type == OctreeNodeType.Node_Internal)
                    {
                        isCollapsible = false;
                    }
                    else
                    {
                        qef.add(child.drawInfo.qef);

                        midsign = (child.drawInfo.corners >> (7 - i)) & 1;
                        signs[i] = (child.drawInfo.corners >> i) & 1;
                    }
                }
            }

            if (!isCollapsible)
            {
                // at least one child is an internal node, can't collapse
                return node;
            }

            var qefPosition = Vector3.Zero;
            qef.solve(qefPosition, QEF_ERROR, QEF_SWEEPS, QEF_ERROR);
            var error = qef.getError();

            // convert to glm vec3 for ease of use
            var position = new Vector3(qefPosition.X, qefPosition.Y, qefPosition.Z);

            // at this point the masspoint will actually be a sum, so divide to make it the average
            if (error > threshold)
            {
                // this collapse breaches the threshold
                return node;
            }

            if (position.X < node.min.X || position.X > (node.min.X + node.size) ||
                position.Y < node.min.Y || position.Y > (node.min.Y + node.size) ||
                position.Z < node.min.Z || position.Z > (node.min.Z + node.size))
            {
                position = qef.getMassPoint();
            }

            // change the node from an internal node to a 'psuedo leaf' node

            var drawInfo = _spareDrawInfo.Count > 0 ? _spareDrawInfo.Dequeue() : new OctreeDrawInfo();
            drawInfo.corners = 0;
            drawInfo.index = 0;

            for (var i = 0; i < 8; i++)
            {
                if (signs[i] == -1)
                {
                    // Undetermined, use centre sign instead
                    drawInfo.corners |= (midsign << i);
                }
                else
                {
                    drawInfo.corners |= (signs[i] << i);
                }
            }

            drawInfo.averageNormal = Vector3.Zero;
            for (var i = 0; i < 8; i++)
            {
                if (node.children[i] != null)
                {
                    var child = node.children[i];
                    if (child.Type == OctreeNodeType.Node_Psuedo ||
                        child.Type == OctreeNodeType.Node_Leaf)
                    {
                        drawInfo.averageNormal += child.drawInfo.averageNormal;
                    }
                }
            }

            drawInfo.averageNormal = Vector3.Normalize(drawInfo.averageNormal);
            drawInfo.position = position;
            drawInfo.qef = qef.getData();

            DestroyOctree(node);

            node.Type = OctreeNodeType.Node_Psuedo;
            node.drawInfo = drawInfo;

            return node;
        }

        private static void GenerateVertexIndices(OctreeNode node, List<MeshVertex> vertexBuffer)
        {
            if (node == null)
            {
                return;
            }

            if (node.Type != OctreeNodeType.Node_Leaf)
            {
                for (var i = 0; i < 8; i++)
                {
                    GenerateVertexIndices(node.children[i], vertexBuffer);
                }
            }

            if (node.Type != OctreeNodeType.Node_Internal)
            {
                node.drawInfo.index = (uint) vertexBuffer.Count;

                vertexBuffer.Add(new MeshVertex(node.drawInfo.position, node.drawInfo.averageNormal));
            }
        }

        private static void ContourProcessEdge(OctreeNode[] node, int dir, List<uint> indexBuffer)
        {
            var minSize = 1000000; // arbitrary big number
            var minIndex = 0;
            var indices = new uint[4] {0, 0, 0, 0};
            var flip = false;
            var signChange = new bool[4] {false, false, false, false};

            for (var i = 0; i < 4; i++)
            {
                var edge = processEdgeMask[dir][i];
                var c1 = Edgevmap[edge][0];
                var c2 = Edgevmap[edge][1];

                var m1 = (node[i].drawInfo.corners >> c1) & 1;
                var m2 = (node[i].drawInfo.corners >> c2) & 1;

                if (node[i].size < minSize)
                {
                    minSize = node[i].size;
                    minIndex = i;
                    flip = m1 != MATERIAL_AIR;
                }

                indices[i] = node[i].drawInfo.index;

                signChange[i] =
                    (m1 == MATERIAL_AIR && m2 != MATERIAL_AIR) ||
                    (m1 != MATERIAL_AIR && m2 == MATERIAL_AIR);
            }

            if (signChange[minIndex])
            {
                if (!flip)
                {
                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[1]);
                    indexBuffer.Add(indices[3]);

                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[3]);
                    indexBuffer.Add(indices[2]);
                }
                else
                {
                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[3]);
                    indexBuffer.Add(indices[1]);

                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[2]);
                    indexBuffer.Add(indices[3]);
                }

            }
        }

        private static void ContourEdgeProc(OctreeNode[] node, int dir, List<uint> indexBuffer)
        {
            if (node[0] == null || node[1] == null || node[2] == null || node[3] == null)
            {
                return;
            }

            if (node[0].Type != OctreeNodeType.Node_Internal &&
                node[1].Type != OctreeNodeType.Node_Internal &&
                node[2].Type != OctreeNodeType.Node_Internal &&
                node[3].Type != OctreeNodeType.Node_Internal)
            {
                ContourProcessEdge(node, dir, indexBuffer);
            }
            else
            {
                for (var i = 0; i < 2; i++)
                {
                    var edgeNodes = new OctreeNode[4];
                    var c = new int[4]
                    {
                        edgeProcEdgeMask[dir][i][0],
                        edgeProcEdgeMask[dir][i][1],
                        edgeProcEdgeMask[dir][i][2],
                        edgeProcEdgeMask[dir][i][3],
                    };

                    for (var j = 0; j < 4; j++)
                    {
                        if (node[j].Type == OctreeNodeType.Node_Leaf || node[j].Type == OctreeNodeType.Node_Psuedo)
                        {
                            edgeNodes[j] = node[j];
                        }
                        else
                        {
                            edgeNodes[j] = node[j].children[c[j]];
                        }
                    }

                    ContourEdgeProc(edgeNodes, edgeProcEdgeMask[dir][i][4], indexBuffer);
                }
            }
        }

        private static void ContourFaceProc(OctreeNode[] node, int dir, List<uint> indexBuffer)
        {
            if (node[0] == null || node[1] == null)
            {
                return;
            }

            if (node[0].Type == OctreeNodeType.Node_Internal ||
                node[1].Type == OctreeNodeType.Node_Internal)
            {
                for (var i = 0; i < 4; i++)
                {
                    var faceNodes = new OctreeNode[2];
                    var c = new int[2]
                    {
                        faceProcFaceMask[dir][i][0],
                        faceProcFaceMask[dir][i][1],
                    };

                    for (var j = 0; j < 2; j++)
                    {
                        if (node[j].Type != OctreeNodeType.Node_Internal)
                        {
                            faceNodes[j] = node[j];
                        }
                        else
                        {
                            faceNodes[j] = node[j].children[c[j]];
                        }
                    }

                    ContourFaceProc(faceNodes, faceProcFaceMask[dir][i][2], indexBuffer);
                }

                var orders = new int[2][]
                {
                    new int[4] {0, 0, 1, 1},
                    new int[4] {0, 1, 0, 1},
                };

                for (var i = 0; i < 4; i++)
                {
                    var edgeNodes = new OctreeNode[4];
                    var c = new int[4]
                    {
                        faceProcEdgeMask[dir][i][1],
                        faceProcEdgeMask[dir][i][2],
                        faceProcEdgeMask[dir][i][3],
                        faceProcEdgeMask[dir][i][4],
                    };

                    var order = orders[faceProcEdgeMask[dir][i][0]];
                    for (var j = 0; j < 4; j++)
                    {
                        if (node[order[j]].Type == OctreeNodeType.Node_Leaf ||
                            node[order[j]].Type == OctreeNodeType.Node_Psuedo)
                        {
                            edgeNodes[j] = node[order[j]];
                        }
                        else
                        {
                            edgeNodes[j] = node[order[j]].children[c[j]];
                        }
                    }

                    ContourEdgeProc(edgeNodes, faceProcEdgeMask[dir][i][5], indexBuffer);
                }
            }
        }

        private static void ContourCellProc(OctreeNode node, List<uint> indexBuffer)
        {
            if (node == null)
            {
                return;
            }

            if (node.Type == OctreeNodeType.Node_Internal)
            {
                for (var i = 0; i < 8; i++)
                {
                    ContourCellProc(node.children[i], indexBuffer);
                }

                for (var i = 0; i < 12; i++)
                {
                    var faceNodes = new OctreeNode[2];
                    int[] c = {cellProcFaceMask[i][0], cellProcFaceMask[i][1]};

                    faceNodes[0] = node.children[c[0]];
                    faceNodes[1] = node.children[c[1]];

                    ContourFaceProc(faceNodes, cellProcFaceMask[i][2], indexBuffer);
                }

                for (var i = 0; i < 6; i++)
                {
                    var edgeNodes = new OctreeNode[4];
                    var c = new int[4]
                    {
                        cellProcEdgeMask[i][0],
                        cellProcEdgeMask[i][1],
                        cellProcEdgeMask[i][2],
                        cellProcEdgeMask[i][3],
                    };

                    for (var j = 0; j < 4; j++)
                    {
                        edgeNodes[j] = node.children[c[j]];
                    }

                    ContourEdgeProc(edgeNodes, cellProcEdgeMask[i][4], indexBuffer);
                }
            }
        }

        private static Vector3 ApproximateZeroCrossingPosition(Vector3 p0, Vector3 p1, Func<Vector3, float> densityFunc)
        {
            // approximate the zero crossing by finding the min value along the edge
            var d0 = densityFunc(p0);
            var d1 = densityFunc(p1);
            d1 -= d0;
            var mid = 0 - d0;
            return p0 + ((p1 - p0) * (mid / d1));
        }

        public static int NormalCount, CrossingPosition, Leaf;

        private static Vector3 CalculateSurfaceNormal(Vector3 p, Func<Vector3, float> densityFunc)
        {
            var H = 0.1f;
            //float H = 1;
            NormalCount += 6;
            var dx = densityFunc(p + new Vector3(H, 0.0f, 0.0f)) - densityFunc(p - new Vector3(H, 0.0f, 0.0f));
            var dy = densityFunc(p + new Vector3(0.0f, H, 0.0f)) - densityFunc(p - new Vector3(0.0f, H, 0.0f));
            var dz = densityFunc(p + new Vector3(0.0f, 0.0f, H)) - densityFunc(p - new Vector3(0.0f, 0.0f, H));

            return Vector3.Normalize(new Vector3(dx, dy, dz));
        }

        public static Stopwatch sw;

        private static OctreeNode ConstructOctreeNodes(OctreeNode node, Func<Vector3, float> densityFunc)
        {
            if (node == null)
            {
                return null;
            }

            if (node.size <= 1)
            {
                var constructOctreeNodes = ConstructLeaf(node, densityFunc);
                return constructOctreeNodes;
            }
            sw.Start();
            Leaf++;
            var density = densityFunc(node.min) > 0;
            var noEdge = true;
            for (var x = -2; x <= node.size + 1; x++)
            {
                for (var y = -2; y <= node.size + 1; y++)
                {
                    for (var z = -2; z <= node.size + 1; z++)
                    {
                        var pos = node.min + new Vector3(x, y, z);
                        Leaf++;
                        if (density != densityFunc(pos) > 0)
                        {
                            noEdge = false;
                            break;
                        }
                    }
                }
            }
            sw.Stop();
            if (noEdge)
            {
                return null;
            }

            var childSize = node.size / 2;
            var hasChildren = false;

            for (var i = 0; i < 8; i++)
            {
                var child = _spareNodes.Count > 0 ? _spareNodes.Dequeue() : new OctreeNode();
                child.size = childSize;
                child.min = node.min + (CHILD_MIN_OFFSETS[i] * childSize);
                child.Type = OctreeNodeType.Node_Internal;

                node.children[i] = ConstructOctreeNodes(child, densityFunc);
                hasChildren |= (node.children[i] != null);
            }

            if (!hasChildren)
            {
                return null;
            }

            return node;
        }

        private static OctreeNode ConstructLeaf(OctreeNode leaf, Func<Vector3, float> densityFunc)
        {
            if (leaf == null || leaf.size > 1)
            {
                return null;
            }

            var corners = 0;
            for (var i = 0; i < 8; i++)
            {
                var cornerPos = leaf.min + CHILD_MIN_OFFSETS[i];
                Leaf++;
                var density = densityFunc(cornerPos);
                var material = density < 0.0f ? MATERIAL_SOLID : MATERIAL_AIR;
                corners |= (material << i);
            }

            if (corners == 0 || corners == 255)
            {
                // voxel is full inside or outside the volume
                return null;
            }

            // otherwise the voxel contains the surface, so find the edge intersections
            const int MAX_CROSSINGS = 6;
            var edgeCount = 0;
            var averageNormal = Vector3.Zero;
            var qef = new QefSolver();

            for (var i = 0; i < 12 && edgeCount < MAX_CROSSINGS; i++)
            {
                var c1 = Edgevmap[i][0];
                var c2 = Edgevmap[i][1];

                var m1 = (corners >> c1) & 1;
                var m2 = (corners >> c2) & 1;

                if ((m1 == MATERIAL_AIR && m2 == MATERIAL_AIR) || (m1 == MATERIAL_SOLID && m2 == MATERIAL_SOLID))
                {
                    // no zero crossing on this edge
                    continue;
                }

                var p1 = leaf.min + CHILD_MIN_OFFSETS[c1];
                var p2 = leaf.min + CHILD_MIN_OFFSETS[c2];
                var p = ApproximateZeroCrossingPosition(p1, p2, densityFunc);
                var n = CalculateSurfaceNormal(p, densityFunc);
                qef.add(p.X, p.Y, p.Z, n.X, n.Y, n.Z);

                averageNormal += n;

                edgeCount++;
            }

            var qefPosition = Vector3.Zero;
            qef.solve(qefPosition, QEF_ERROR, QEF_SWEEPS, QEF_ERROR);

            var drawInfo = _spareDrawInfo.Count > 0 ? _spareDrawInfo.Dequeue() : new OctreeDrawInfo();
            drawInfo.corners = 0;
            drawInfo.index = 0;
            drawInfo.position = new Vector3(qefPosition.X, qefPosition.Y, qefPosition.Z);
            drawInfo.qef = qef.getData();

            var min = leaf.min;
            var max = new Vector3(leaf.min.X + leaf.size, leaf.min.Y + leaf.size, leaf.min.Z + leaf.size);
            if (drawInfo.position.X < min.X || drawInfo.position.X > max.X ||
                drawInfo.position.Y < min.Y || drawInfo.position.Y > max.Y ||
                drawInfo.position.Z < min.Z || drawInfo.position.Z > max.Z)
            {
                drawInfo.position = qef.getMassPoint();
            }

            drawInfo.averageNormal = Vector3.Normalize(averageNormal / (float) edgeCount);
            drawInfo.corners = corners;

            leaf.Type = OctreeNodeType.Node_Leaf;
            leaf.drawInfo = drawInfo;

            return leaf;
        }

        public static OctreeNode BuildOctree(Vector3 min, int size, float threshold, Func<Vector3, float> densityFunc)
        {
            Debug.WriteLine("Building Octree at {0}, with size of {1} and threshold of {2}", min, size, threshold);

            var field = new int[65 * 65 * 65];
            for (var x = 0; x < 65; x++)
            for (var y = 0; y < 65; y++)
            for (var z = 0; z < 65; z++)
            {
                GenerateDefaultField(new Vector3(x, y, z), densityFunc, field);
            }
            var root = new OctreeNode
            {
                min = min,
                size = size,
                Type = OctreeNodeType.Node_Internal
            };
            root = ConstructOctreeNodes(root, densityFunc);
            root = SimplifyOctree(root, threshold);

            return root;
        }

        private static void GenerateDefaultField(Vector3 position, Func<Vector3, float> densityFunc, int[] field)
        {
            var index = (int) (position.X + (position.Y * 65) + (position.Z * 65 * 65));
            field[index] = densityFunc(position) < 0 ? MATERIAL_SOLID : MATERIAL_AIR;
        }

        public static OctreeNode BuildOctree(OctreeNode root,
                                             float threshold,
                                             Vector3 position,
                                             Vector3 size,
                                             Func<Vector3, float> densityFunc)
        {
            Debug.WriteLine("Updating Octree node");

            root = ConstructOctreeNodes(root, densityFunc);
            root = SimplifyOctree(root, threshold);

            return root;
        }

        public static void GenerateMeshFromOctree(OctreeNode node, Action<Vertex[], uint[]> buildMesh)
        {
            if (node == null)
            {
                return;
            }

            var vertexBuffer = new List<MeshVertex>();
            var indexBuffer = new List<uint>();

            GenerateVertexIndices(node, vertexBuffer);
            ContourCellProc(node, indexBuffer);

            var verts = new Vertex[vertexBuffer.Count];
            for (var i = 0; i < vertexBuffer.Count; i++)
            {
                var uv = new Vector2(vertexBuffer[i].xyz.X, vertexBuffer[i].xyz.Z);
                verts[i] = new Vertex(vertexBuffer[i].xyz, uv, vertexBuffer[i].normal);
            }

            buildMesh(verts.ToArray(), indexBuffer.ToArray());
        }

        public static void DrawOctree(OctreeNode rootNode, int colorIndex, Matrix4x4 transform)
        {
            if (rootNode == null || rootNode.children.Length <= 0) return;

            foreach (var t in rootNode.children)
            {
                DrawOctree(t, colorIndex + 1, transform);
            }
        }
    
        public static void DestroyOctree(OctreeNode node)
        {
            if (node == null)
            {
                return;
            }

            for (var i = 0; i < 8; i++)
            {
                DestroyOctree(node.children[i]);
                if (node.children[i] != null && node.children[i].drawInfo != null)
                {
                    _spareDrawInfo.Enqueue(node.children[i].drawInfo);
                    node.children[i].drawInfo = null;
                }
                _spareNodes.Enqueue(node.children[i]);
                node.children[i] = null;
            }
        }
    }
}