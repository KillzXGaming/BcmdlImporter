using System;
using SPICA.Formats.CtrGfx;
using Newtonsoft.Json;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.CtrGfx.Texture;
using SPICA.Formats.CtrGfx.Model.Material;

namespace BcmdlImporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Usage:");
                Console.WriteLine($"Extract: ModelImporter.exe (input bcmdl)");
                Console.WriteLine($"Import: ModelImporter.exe (input bcmdl) (folder path of extracted bcmdl)");
                return;
            }

            var bcres = Gfx.Open(args[0]);
            bcres.MaterialAnimations.Clear();
            bcres.SkeletalAnimations.Clear();
            bcres.CameraAnimations.Clear();

            if (args.Length == 1)
            {
                string folder = Path.GetFileNameWithoutExtension(args[0]);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                for (int i = 0; i < bcres.Models.Count; i++)
                    ModelTools.Export(bcres, i, folder);
                return;
            }

            if (Directory.Exists(args[1]))
            {
                bcres.Textures.Clear();
                foreach (var file in Directory.GetFiles(Path.Combine(args[1], "Textures")))
                {
                    if (file.EndsWith(".png"))
                    {
                        Console.WriteLine($"Importing texture {Path.GetFileName(file)}");

                        string metaFile = file.Replace(".png", ".json");
                        if (File.Exists(metaFile))
                        {
                            TextureMeta meta = JsonConvert.DeserializeObject<TextureMeta>(File.ReadAllText(metaFile));
                            var format = Enum.Parse<SPICA.PICA.Commands.PICATextureFormat>(meta.Format);
                            bool useMips = true;
                            var h3dTexture = new H3DTexture(file, format, useMips ? meta.MipCount : 1);
                            bcres.Textures.Add(GfxTexture.FromH3D(h3dTexture));
                        }
                        else
                        {
                            var h3dTexture = new H3DTexture(file, SPICA.PICA.Commands.PICATextureFormat.ETC1A4);
                            bcres.Textures.Add(GfxTexture.FromH3D(h3dTexture));
                        }
                    }
                }

                foreach (var modelFolder in Directory.GetDirectories(Path.Combine(args[1], "Models")))
                {
                    foreach (var file in Directory.GetFiles(modelFolder))
                    {
                        if (file.EndsWith(".dae") || file.EndsWith(".fbx") || file.EndsWith(".obj"))
                        {
                            string name = new DirectoryInfo(modelFolder).Name;
                            int modelIndex = bcres.Models.Find(name);
                            if (modelIndex == -1)
                                modelIndex = 0;

                            var model = bcres.Models[modelIndex];

                            model.Materials.Clear();

                            foreach (var matFile in Directory.GetFiles(Path.Combine(modelFolder, "Materials")))
                            {
                                var material = JsonConvert.DeserializeObject<GfxMaterial>(File.ReadAllText(matFile));
                                material.Name = Path.GetFileNameWithoutExtension(matFile);
                                model.Materials.Add(material);

                                for (int i = 0; i < 3; i++)
                                {
                                    if (material.TextureMappers[i] != null && material.TextureMappers[i].Texture.Name != "")
                                    {
                                        string tex = material.TextureMappers[i].Texture.Name;
                                        if (!bcres.Textures.Contains(tex))
                                            Console.WriteLine($"Cannot find {tex} from material {material.Name} inside the textures folder!");
                                    }
                                }
                            }

                            var newModel = ModelTools.Import(file, model);
                            bcres.Models[modelIndex] = newModel;
                        }
                    }
                }
            }
            Gfx.Save($"{args[0]}RB.bcmdl", bcres);

          //  Gfx.Open($"{args[0]}RB.bcmdl");
        }
    }
}