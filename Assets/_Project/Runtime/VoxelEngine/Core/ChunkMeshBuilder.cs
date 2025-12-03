using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Costruisce la mesh ottimizzata per un chunk utilizzando:
    /// - Greedy Meshing Algorithm
    /// - Face Culling
    /// VERSIONE SEMPLIFICATA (senza Jobs/Burst per compatibilità)
    /// </summary>
    public static class ChunkMeshBuilder
    {
        // ═══════════════════════════════════════════════════════════
        // COSTANTI
        // ═══════════════════════════════════════════════════════════
        
        private static readonly Vector3[] FACE_NORMALS = new Vector3[6]
        {
            new Vector3(0, 1, 0),   // Top
            new Vector3(0, -1, 0),  // Bottom
            new Vector3(0, 0, 1),   // Front
            new Vector3(0, 0, -1),  // Back
            new Vector3(1, 0, 0),   // Right
            new Vector3(-1, 0, 0)   // Left
        };
        
        // ═══════════════════════════════════════════════════════════
        // BUILD MESH (Main Entry Point)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Costruisce la mesh del chunk.
        /// Ritorna true se la mesh è stata generata con successo.
        /// </summary>
        public static bool BuildChunkMesh(Chunk chunk, MeshFilter meshFilter, MeshCollider meshCollider, BlockRegistry blockRegistry)
        {
            if (chunk == null || chunk.Blocks == null)
                return false;
            
            // Liste temporanee per costruzione mesh
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            
            // Genera facce per ogni direzione usando greedy meshing
            for (int direction = 0; direction < 6; direction++)
            {
                GenerateFacesForDirection(chunk, direction, vertices, normals, uvs, triangles);
            }
            
            // Crea o pulisci mesh
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = $"ChunkMesh_{chunk.chunkPosition.x}_{chunk.chunkPosition.y}";
            }
            else
            {
                mesh.Clear();
            }
            
            // Assegna dati
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            
            // Ricalcola bounds
            mesh.RecalculateBounds();
            
            // Assegna mesh
            meshFilter.sharedMesh = mesh;
            
            // Aggiorna collider se presente
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
            }
            
            return true;
        }
        
        
        // ═══════════════════════════════════════════════════════════
        // GREEDY MESHING PER DIREZIONE
        // ═══════════════════════════════════════════════════════════
        
        private static void GenerateFacesForDirection(
            Chunk chunk,
            int direction,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            Vector3Int normal = GetDirectionVector(direction);
            
            // Determina assi per la direzione corrente
            int d = GetMainAxis(direction);
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;
            
            Vector3Int dims = new Vector3Int(Chunk.CHUNK_WIDTH, Chunk.CHUNK_HEIGHT, Chunk.CHUNK_DEPTH);
            Vector3Int x = Vector3Int.zero;
            
            // Maschera per greedy meshing
            int[,] mask = new int[dims[u], dims[v]];
            
            // Scansiona tutte le slice lungo l'asse principale
            for (x[d] = -1; x[d] < dims[d];)
            {
                // Calcola maschera
                for (x[v] = 0; x[v] < dims[v]; x[v]++)
                {
                    for (x[u] = 0; x[u] < dims[u]; x[u]++)
                    {
                        // Blocco corrente e blocco vicino
                        VoxelBlock blockCurrent = GetBlock(chunk, x);
                        VoxelBlock blockCompare = GetBlock(chunk, x + normal);
                        
                        // Determina se serve una faccia
                        bool currentSolid = x[d] >= 0 && !blockCurrent.IsAir;
                        bool compareSolid = x[d] < dims[d] - 1 && !blockCompare.IsAir;
                        
                        if (currentSolid == compareSolid)
                        {
                            mask[x[u], x[v]] = 0; // Nessuna faccia
                        }
                        else if (currentSolid)
                        {
                            mask[x[u], x[v]] = (int)blockCurrent.blockID;
                        }
                        else
                        {
                            mask[x[u], x[v]] = -(int)blockCompare.blockID;
                        }
                    }
                }
                
                x[d]++;
                
                // Genera mesh dalla maschera usando greedy algorithm
                for (int j = 0; j < dims[v]; j++)
                {
                    for (int i = 0; i < dims[u];)
                    {
                        if (mask[i, j] != 0)
                        {
                            // Calcola larghezza
                            int currentMask = mask[i, j];
                            int w;
                            for (w = 1; i + w < dims[u] && mask[i + w, j] == currentMask; w++) { }
                            
                            // Calcola altezza
                            bool done = false;
                            int h;
                            for (h = 1; j + h < dims[v]; h++)
                            {
                                for (int k = 0; k < w; k++)
                                {
                                    if (mask[i + k, j + h] != currentMask)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (done) break;
                            }
                            
                            // Crea quad
                            x[u] = i;
                            x[v] = j;
                            
                            Vector3Int du = Vector3Int.zero;
                            du[u] = w;
                            
                            Vector3Int dv = Vector3Int.zero;
                            dv[v] = h;
                            
                            AddQuad(
                                new Vector3(x.x, x.y, x.z),
                                new Vector3(du.x, du.y, du.z),
                                new Vector3(dv.x, dv.y, dv.z),
                                FACE_NORMALS[direction],
                                Mathf.Abs(currentMask),
                                currentMask > 0,
                                vertices,
                                normals,
                                uvs,
                                triangles
                            );
                            
                            // Azzera maschera per area processata
                            for (int l = 0; l < h; l++)
                            {
                                for (int k = 0; k < w; k++)
                                {
                                    mask[i + k, j + l] = 0;
                                }
                            }
                            
                            i += w;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY METHODS
        // ═══════════════════════════════════════════════════════════
        
        private static void AddQuad(
            Vector3 origin,
            Vector3 du,
            Vector3 dv,
            Vector3 normal,
            int blockID,
            bool isPositive,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            int vertexIndex = vertices.Count;
            
            // Offset per facce back (evita z-fighting)
            Vector3 offset = isPositive ? normal : Vector3.zero;
            
            // 4 vertici del quad
            vertices.Add(origin + offset);
            vertices.Add(origin + du + offset);
            vertices.Add(origin + du + dv + offset);
            vertices.Add(origin + dv + offset);
            
            // Normali
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
            }
            
            // UV (da mappare con texture atlas)
            float uvScale = 0.0625f; // 16x16 atlas = 1/16
            float uvIndex = (blockID - 1) * uvScale;
            
            uvs.Add(new Vector2(uvIndex, 0));
            uvs.Add(new Vector2(uvIndex + uvScale, 0));
            uvs.Add(new Vector2(uvIndex + uvScale, 1));
            uvs.Add(new Vector2(uvIndex, 1));
            
            // Triangoli (winding order dipende dalla direzione)
            if (isPositive)
            {
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
            }
            else
            {
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);
            }
        }
        
        private static VoxelBlock GetBlock(Chunk chunk, Vector3Int pos)
        {
            if (pos.x < 0 || pos.x >= Chunk.CHUNK_WIDTH ||
                pos.y < 0 || pos.y >= Chunk.CHUNK_HEIGHT ||
                pos.z < 0 || pos.z >= Chunk.CHUNK_DEPTH)
            {
                return new VoxelBlock(0); // Aria fuori bounds
            }
            
            return chunk.GetBlock(pos.x, pos.y, pos.z);
        }
        
        private static Vector3Int GetDirectionVector(int direction)
        {
            switch (direction)
            {
                case 0: return new Vector3Int(0, 1, 0);   // Top
                case 1: return new Vector3Int(0, -1, 0);  // Bottom
                case 2: return new Vector3Int(0, 0, 1);   // Front
                case 3: return new Vector3Int(0, 0, -1);  // Back
                case 4: return new Vector3Int(1, 0, 0);   // Right
                case 5: return new Vector3Int(-1, 0, 0);  // Left
                default: return Vector3Int.zero;
            }
        }
        
        private static int GetMainAxis(int direction)
        {
            if (direction <= 1) return 1; // Y axis
            if (direction <= 3) return 2; // Z axis
            return 0; // X axis
        }
    }
}
