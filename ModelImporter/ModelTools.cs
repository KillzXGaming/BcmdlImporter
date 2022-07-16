using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using IONET;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.PICA;
using Newtonsoft.Json;

namespace BcmdlImporter

{
    internal class ModelTools
    {
        public class BcresImportSettings
        {

        }

        public static void Export(Gfx gfx, int modelIndex, string folder)
        {
            string modelFolder = Path.Combine(folder, Path.Combine("Models", $"{gfx.Models[modelIndex].Name}"));
            string texFolder = Path.Combine(folder, "Textures");

            if (!Directory.Exists(modelFolder)) Directory.CreateDirectory(modelFolder);

            var h3d = gfx.ToH3D();
            var collada = new SPICA.Formats.Generic.COLLADA.DAE(h3d, modelIndex);
            collada.Save(Path.Combine(modelFolder, $"{gfx.Models[modelIndex].Name}.dae"));

            string matFolder = Path.Combine(modelFolder, "Materials");

            if (!Directory.Exists(matFolder)) Directory.CreateDirectory(matFolder);
            if (!Directory.Exists(texFolder)) Directory.CreateDirectory(texFolder);

            foreach (var mat in gfx.Models[modelIndex].Materials)
            {
                string json = JsonConvert.SerializeObject(mat, Formatting.Indented);
                File.WriteAllText(Path.Combine(matFolder, $"{mat.Name}.json"), json);
            }
            foreach (var tex in gfx.Textures)
            {
                TextureMeta texMeta = new TextureMeta();
                texMeta.Format = tex.HwFormat.ToString();
                texMeta.MipCount = tex.MipmapSize;
                texMeta.MetaData = tex.MetaData;

                string json = JsonConvert.SerializeObject(texMeta, Formatting.Indented);
                File.WriteAllText(Path.Combine(texFolder, $"{tex.Name}.json"), json);

                var h3dTex = tex.ToH3D();
                h3dTex.ToBitmap().Save(Path.Combine(texFolder, $"{tex.Name}.png"));
            }
        }

        public static GfxModel Import(string filePath, GfxModel parent)
        {
            bool importBones = false;

            var scene = IOManager.LoadScene(filePath, new ImportSettings());
            var model = scene.Models[0];

            var bones = model.Skeleton.BreathFirstOrder();
            bool hasBones = parent is GfxModelSkeletal || bones.Count > 1;

            var gfxModel = hasBones ? new GfxModelSkeletal()
            {
                Materials = parent.Materials,
                MetaData = parent.MetaData,
                AnimationsGroup = parent.AnimationsGroup,
                Childs = parent.Childs,
                FaceCulling = parent.FaceCulling,
                Flags = parent.Flags,
                LayerId = parent.LayerId,
                IsBranchVisible = parent.IsBranchVisible,
                TransformScale = parent.TransformScale,
                TransformRotation = parent.TransformRotation,
                TransformTranslation = parent.TransformTranslation,
                LocalTransform = parent.LocalTransform,
                WorldTransform = parent.WorldTransform,
                MeshNodeVisibilities = parent.MeshNodeVisibilities,
                Name = parent.Name,
            } : new GfxModel()
            {
                Materials = parent.Materials,
                MetaData = parent.MetaData,
                AnimationsGroup = parent.AnimationsGroup,
                Childs = parent.Childs,
                FaceCulling = parent.FaceCulling,
                Flags = parent.Flags,
                LayerId = parent.LayerId,
                IsBranchVisible = parent.IsBranchVisible,
                TransformScale = parent.TransformScale,
                TransformRotation = parent.TransformRotation,
                TransformTranslation = parent.TransformTranslation,
                LocalTransform = parent.LocalTransform,
                WorldTransform = parent.WorldTransform,
                MeshNodeVisibilities = parent.MeshNodeVisibilities,
                Name = parent.Name,
            };
            gfxModel.Name = Path.GetFileNameWithoutExtension(filePath);

            if (gfxModel is GfxModelSkeletal)
                ((GfxModelSkeletal)gfxModel).Skeleton = ((GfxModelSkeletal)parent).Skeleton;

            model.Name = Path.GetFileNameWithoutExtension(filePath);
            Console.WriteLine($"Importing model {model.Name}");

            if (importBones)
            {
            }
            else if (parent is GfxModelSkeletal)
            {
                ((GfxModelSkeletal)gfxModel).Skeleton = ((GfxModelSkeletal)parent).Skeleton;

                GfxSkeleton skeleton = new GfxSkeleton();
                ((GfxModelSkeletal)gfxModel).Skeleton = skeleton;
                skeleton.Name = "";
                skeleton.ScalingRule = GfxSkeletonScalingRule.Maya;

                foreach (var bn in ((GfxModelSkeletal)parent).Skeleton.Bones)
                    skeleton.Bones.Add(bn);

                skeleton.Bones[0] = new GfxBone()
                {
                    Name = "Nw4cRootTool",
                    MetaData = new GfxDict<GfxMetaData>(),
                    Translation = new Vector3(),
                    Scale = new Vector3(1, 1, 1),
                    Rotation = new Vector3(),
                    BillboardMode = GfxBillboardMode.Off,
                    Flags = GfxBoneFlags.IsIdentity,
                    Index = 0,
                    WorldTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                    InvWorldTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                    LocalTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                };
            }

            Matrix4x4[] skinningMatrices = new Matrix4x4[0];
            if (gfxModel is GfxModelSkeletal)
            {
                var skeleton = ((GfxModelSkeletal)gfxModel).Skeleton;

                skinningMatrices = new Matrix4x4[skeleton.Bones.Count];
                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    var bn = skeleton.Bones[i];
                    var mat = GetWorldTransform(skeleton.Bones, bn);
                    Matrix4x4.Invert(mat, out Matrix4x4 inverted);
                    skinningMatrices[i] = inverted;
                }
                //Update transforms by skeleton
            }

            var meshes = CleanupMeshes(model.Meshes);
            foreach (var mat in gfxModel.Materials)
            {
                var iomaterial = scene.Materials.FirstOrDefault(x => x.Label == mat.Name);
                if (iomaterial != null && iomaterial.DiffuseMap != null)
                {
                    var texture = Path.GetFileName(iomaterial.DiffuseMap.FilePath).Replace(".png", "");
                    if (!string.IsNullOrEmpty(texture))
                    {
                        Console.WriteLine($"Mapping {texture} to diffuse at slot 0");
                        mat.TextureMappers[0].Texture.Name = texture;
                        mat.TextureMappers[0].Texture.Path = texture;
                    }
                }
            }
            foreach (var mesh in meshes)
                ConvertMesh(scene, mesh, gfxModel, skinningMatrices);
            return gfxModel;
        }

        static List<IOMesh> CleanupMeshes(List<IOMesh> meshes)
        {
            List<string> input = new List<string>();

            List<IOMesh> newList = new List<IOMesh>();
            foreach (var mesh in meshes)
            {
                if (input.Contains(mesh.Polygons[0].MaterialName))
                    continue;

                input.Add(mesh.Polygons[0].MaterialName);
                newList.Add(mesh);

                //Combine meshes by polygons and vertices
                var meshDupes = meshes.Where(x => x.Polygons[0].MaterialName == mesh.Polygons[0].MaterialName).ToList();
                if (meshDupes.Count > 1)
                {
                    foreach (var msh in meshDupes)
                    {
                        if (msh == mesh)
                            continue;

                        IOPolygon poly = mesh.Polygons[0];
                        //poly.MaterialName = mesh.Polygons[0].MaterialName;
                       // mesh.Polygons.Add(poly);

                        foreach (var p in msh.Polygons)
                        {
                            Dictionary<IOVertex, int> remapVertex = new Dictionary<IOVertex, int>();
                            for (int i = 0; i < p.Indicies.Count; i++)
                            {
                                var v = msh.Vertices[p.Indicies[i]];
                                if (!remapVertex.ContainsKey(v))
                                {
                                    remapVertex.Add(v, mesh.Vertices.Count);
                                    mesh.Vertices.Add(v);
                                }
                                poly.Indicies.Add(remapVertex[v]);
                            }
                            remapVertex.Clear();
                        }
                    }
                    mesh.Optimize();
                }
            }
            return newList;
        }

        static Matrix4x4 GetWorldTransform(GfxDict<GfxBone> skeleton, GfxBone bn)
        {
            if (bn.Parent != null && skeleton.Contains(bn.Parent.Name))
                return GetTransform(bn) * GetWorldTransform(skeleton, skeleton[bn.Parent.Name]);
            return GetTransform(bn);
        }

        static Matrix4x4 GetTransform(GfxBone bn)
        {
            var trans = Matrix4x4.CreateTranslation(bn.Translation);
            var sca = Matrix4x4.CreateScale(bn.Scale);
            var rot = Matrix4x4.CreateRotationX(bn.Rotation.X) *
                      Matrix4x4.CreateRotationY(bn.Rotation.Y) *
                      Matrix4x4.CreateRotationZ(bn.Rotation.Z);
            return sca * rot * trans;
        }

        private static void ConvertMesh(IONET.Core.IOScene scene, IOMesh iomesh, GfxModel gfxModel, Matrix4x4[] skinningMatrices)
        {
            GfxMesh gfxMesh = new GfxMesh();
            GfxShape gfxShape = new GfxShape();

            int skinningCount = 0;
            bool rigid = false;
            var attributes = CreateAttributes(iomesh, skinningCount);
            var vertices = GetPICAVertices(iomesh.Vertices, skinningMatrices, gfxModel, rigid).ToArray();

            gfxMesh.MaterialIndex = 0;

            foreach (var poly in iomesh.Polygons)
            {
                var subMeshes = GenerateSubMeshes(iomesh, poly, skinningCount, ref vertices);
                gfxShape.SubMeshes.AddRange(subMeshes);

                var mat = scene.Materials.FirstOrDefault(x => x.Name == poly.MaterialName);
                if (mat != null)
                {
                    var index = gfxModel.Materials.Find(mat.Label);
                    if (index != -1)
                        gfxMesh.MaterialIndex = index;
                }
                else
                    Console.WriteLine($"Cannot find material {poly.MaterialName}!");
            }

            var vertexBuffer = new GfxVertexBufferInterleaved();
            vertexBuffer.AttrName =  PICAAttributeName.Interleave;
            vertexBuffer.Type = GfxVertexBufferType.Interleaved;
            vertexBuffer.Attributes.AddRange(attributes);
            vertexBuffer.VertexStride = attributes.Sum(x => x.Elements * GetStride(x.Format));
            vertexBuffer.RawBuffer = VerticesConverter.GetBuffer(vertices, attributes.Select(x => x.ToPICAAttribute()));
            gfxShape.VertexBuffers.Add(vertexBuffer);
            gfxShape.BlendShape = new GfxBlendShape();
            gfxShape.Name = "";

            if (!iomesh.HasColorSet(0))
            {
                gfxShape.VertexBuffers.Add(new GfxVertexBufferFixed()
                {
                    AttrName = PICAAttributeName.Color,
                    Elements = 4,
                    Format = GfxGLDataType.GL_FLOAT,
                    Scale = 1.0f,
                    Type = GfxVertexBufferType.Fixed,
                    Vector = new float[4] { 1, 1, 1, 1 }
                });
            }
            
            if (!iomesh.HasTangents)
            {
                gfxShape.VertexBuffers.Add(new GfxVertexBufferFixed()
                {
                    AttrName = PICAAttributeName.Tangent,
                    Elements = 3,
                    Format = GfxGLDataType.GL_FLOAT,
                    Scale = 1.0f,
                    Type = GfxVertexBufferType.Fixed,
                    Vector = new float[3] { 0, 1, 0 }
                });
            }

            gfxMesh.Name = "";
            gfxMesh.ShapeIndex = gfxModel.Shapes.Count;
            gfxMesh.PrimitiveIndex = 0;
            gfxMesh.Parent = gfxModel;
            gfxMesh.MeshNodeName = iomesh.Name.Split("_").FirstOrDefault();
            gfxMesh.IsVisible = true;
            gfxMesh.RenderPriority = 0;
            gfxMesh.MeshNodeIndex = -1;

            CalculateBounding(ref gfxShape, iomesh);

            gfxModel.Meshes.Add(gfxMesh);
            gfxModel.Shapes.Add(gfxShape);
        }

        static void CalculateBounding(ref GfxShape gfxShape, IOMesh iomesh)
        {
            //Calculate AABB
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < iomesh.Vertices.Count; i++)
            {
                minX = Math.Min(minX, iomesh.Vertices[i].Position.X);
                minY = Math.Min(minY, iomesh.Vertices[i].Position.Y);
                minZ = Math.Min(minZ, iomesh.Vertices[i].Position.Z);
                maxX = Math.Max(maxX, iomesh.Vertices[i].Position.X);
                maxY = Math.Max(maxY, iomesh.Vertices[i].Position.Y);
                maxZ = Math.Max(maxZ, iomesh.Vertices[i].Position.Z);
            }

            Vector3 max = new Vector3(maxX, maxY, maxZ);
            Vector3 min = new Vector3(minX, minY, minZ);
            gfxShape.BoundingBox.Center = min + ((max - min) / 2);
            gfxShape.BoundingBox.Size = max - min;
        }

        static List<GfxSubMesh> GenerateSubMeshes(IOMesh mesh, IOPolygon poly, int skinningCount, ref PICAVertex[] vertices, int max_bones = 20)
        {
            List<ushort> faces = new List<ushort>();
            foreach (var index in poly.Indicies)
                faces.Add((ushort)index);

            Queue<ushort> IndicesQueue = new Queue<ushort>(faces);
            List<GfxSubMesh> subMeshes = new List<GfxSubMesh>();

            //Split the mesh into sub meshes based on the max amount of bones used
            while (IndicesQueue.Count > 0)
            {
                int Count = IndicesQueue.Count / 3;

                List<ushort> Indices = new List<ushort>();
                List<int> Bones = new List<int>();

                while (Count-- > 0)
                {
                    ushort i0 = IndicesQueue.Dequeue();
                    ushort i1 = IndicesQueue.Dequeue();
                    ushort i2 = IndicesQueue.Dequeue();

                    List<int> TempBones = new List<int>(12);
                    for (int j = 0; j < mesh.Vertices[i0].Envelope.Weights.Count; j++)
                    {
                        var b0 = vertices[i0].Indices[j];
                        var b1 = vertices[i1].Indices[j];
                        var b2 = vertices[i2].Indices[j];

                        if (b0 != -1 && (!(Bones.Contains(b0) || TempBones.Contains(b0)))) TempBones.Add(b0);
                        if (b1 != -1 && (!(Bones.Contains(b1) || TempBones.Contains(b1)))) TempBones.Add(b1);
                        if (b2 != -1 && (!(Bones.Contains(b2) || TempBones.Contains(b2)))) TempBones.Add(b2);
                    }

                    if (Bones.Count + TempBones.Count > max_bones)
                    {
                        IndicesQueue.Enqueue(i0);
                        IndicesQueue.Enqueue(i1);
                        IndicesQueue.Enqueue(i2);
                    }
                    else
                    {
                        Indices.Add(i0);
                        Indices.Add(i1);
                        Indices.Add(i2);

                        Bones.AddRange(TempBones);
                    }
                }

                GfxSubMesh SM = new GfxSubMesh();
                SM.Skinning = GfxSubMeshSkinning.None;
                if (skinningCount == 1)
                    SM.Skinning = GfxSubMeshSkinning.Rigid;
                if (skinningCount > 1)
                    SM.Skinning = GfxSubMeshSkinning.Smooth;

                bool is16Bit = Indices.Any(x => x > 0xFF);

                GfxFace face = new GfxFace();
                face.FaceDescriptors.Add(new GfxFaceDescriptor()
                {
                    PrimitiveMode = PICAPrimitiveMode.Triangles,
                    Indices = Indices.ToArray(),
                    Format = is16Bit ? GfxGLDataType.GL_UNSIGNED_SHORT : GfxGLDataType.GL_UNSIGNED_BYTE,
                });
                face.Setup();
                SM.Faces.Add(face);

                for (int i = 0; i < Bones.Count; i++)
                    SM.BoneIndices.Add((byte)Bones[i]);

                //Need to atleast bind to a single bone.
                //If no bones are binded, the full model cannot be moved within a map editor if required.
                if (SM.BoneIndices.Count == 0)
                    SM.BoneIndices.Add(0);

                bool[] Visited = new bool[mesh.Vertices.Count];
                foreach (ushort i in Indices)
                {
                    if (!Visited[i])
                    {
                        Visited[i] = true;

                        vertices[i].Indices[0] = Bones.IndexOf(vertices[i].Indices[0]);
                        vertices[i].Indices[1] = Bones.IndexOf(vertices[i].Indices[1]);
                        vertices[i].Indices[2] = Bones.IndexOf(vertices[i].Indices[2]);
                        vertices[i].Indices[3] = Bones.IndexOf(vertices[i].Indices[3]);
                    }
                }
                subMeshes.Add(SM);
            }
            return subMeshes;
        }

        static List<GfxAttribute> CreateAttributes(IOMesh mesh, int skinningCount)
        {
            bool isShortPosition = true;

            int offset = 0;

            List<GfxAttribute> attributes = new List<GfxAttribute>();
            attributes.Add(new GfxAttribute()
            {
                Elements = 3,
                Format = GfxGLDataType.GL_FLOAT,
                AttrName = PICAAttributeName.Position,
                Scale = 1.0f,
                Offset = offset,
            });
            offset += 12;
            if (mesh.HasNormals)
            {
                attributes.Add(new GfxAttribute()
                {
                    Elements = 3,
                    Format = GfxGLDataType.GL_FLOAT,
                    AttrName = PICAAttributeName.Normal,
                    Scale = 1.0f,
                    Offset = offset,
                });
                offset += 12;
            }
            for (int i = 0; i < 3; i++)
            {
                if (mesh.HasUVSet(i))
                {
                    attributes.Add(new GfxAttribute()
                    {
                        Elements = 2,
                        Format = GfxGLDataType.GL_FLOAT,
                        AttrName = (PICAAttributeName)((int)PICAAttributeName.TexCoord0 + i),
                        Scale = 1.0f,
                        Offset = offset,
                    });
                    offset += 8;
                }
            }
            if (mesh.HasColorSet(0))
            {
                attributes.Add(new GfxAttribute()
                {
                    Elements = 4,
                    Format = GfxGLDataType.GL_UNSIGNED_BYTE,
                    AttrName = PICAAttributeName.Color,
                    Scale = 1.0f / 255.0f,
                    Offset = offset,
                });
                offset += 4;
            }
            if (mesh.HasEnvelopes() && false)
            {
                attributes.Add(new GfxAttribute()
                {
                    Elements = skinningCount,
                    Format = GfxGLDataType.GL_SHORT,
                    AttrName = PICAAttributeName.BoneIndex,
                    Scale = 1.0f,
                    Offset = offset,
                });
                offset += skinningCount * 2;
                attributes.Add(new GfxAttribute()
                {
                    Elements = skinningCount,
                    Format = GfxGLDataType.GL_FLOAT,
                    AttrName = PICAAttributeName.BoneWeight,
                    Scale = 1.0f,
                    Offset = offset,
                });
                offset += skinningCount * 4;
            }
            if (mesh.HasTangents)
            {
                attributes.Add(new GfxAttribute()
                {
                    Elements = 3,
                    Format = GfxGLDataType.GL_FLOAT,
                    AttrName = PICAAttributeName.Tangent,
                    Scale = 1.0f,
                    Offset = offset,
                });
                offset += 12;
            }
            return attributes;
        }

        static List<PICAVertex> GetPICAVertices(List<IOVertex> vertices, Matrix4x4[] skinningMatrices, GfxModel model, bool rigid)
        {
            int index = 0;
            List<PICAVertex> verts = new List<PICAVertex>();
            foreach (var vertex in vertices)
            {
                var picaVertex = new PICAVertex();
                picaVertex.Position = new Vector4(vertex.Position.X, vertex.Position.Y, vertex.Position.Z, 1.0f);
                picaVertex.Normal = new Vector4(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z, 1.0f);
                picaVertex.Color = new Vector4(1, 1, 1, 1);

                if (vertex.Colors?.Count > 0)
                    picaVertex.Color = new Vector4(vertex.Colors[0].X, vertex.Colors[0].Y, vertex.Colors[0].Z, vertex.Colors[0].W);
                if (vertex.UVs?.Count > 0)
                    picaVertex.TexCoord0 = new Vector4(vertex.UVs[0].X, vertex.UVs[0].Y, 0, 0);
                if (vertex.UVs?.Count > 1)
                    picaVertex.TexCoord1 = new Vector4(vertex.UVs[1].X, vertex.UVs[1].Y, 0, 0);
                if (vertex.UVs?.Count > 2)
                    picaVertex.TexCoord2 = new Vector4(vertex.UVs[2].X, vertex.UVs[2].Y, 0, 0);
                picaVertex.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1.0f);

                for (int j = 0; j < vertex.Envelope.Weights.Count; j++)
                {
                    var boneWeight = vertex.Envelope.Weights[j];
                }

                verts.Add(picaVertex);
                index++;
            }
            return verts;
        }

        static int GetStride(GfxGLDataType format)
        {
            switch (format)
            {
                case GfxGLDataType.GL_BYTE:
                case GfxGLDataType.GL_UNSIGNED_BYTE:
                    return 1;
                case GfxGLDataType.GL_SHORT:
                case GfxGLDataType.GL_UNSIGNED_SHORT:
                    return 2;
                case GfxGLDataType.GL_FLOAT:
                    return 4;
            }
            return 4;
        }

    }
}
