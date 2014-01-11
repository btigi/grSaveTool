﻿using System;
using System.IO;

namespace grSavTool
{
    class Program
    {
        static void Main(string[] args)
        {
            //args = new string[3];
            //args[0] = "pack";
            //args[1] = @"D:\out.dat";
            //args[2] = @"D:\quicksave.sav";

            var arguments = ParseArguments(args);
            if (arguments.IsValid)
            {
                if (arguments.Operation == Operation.Unpack)
                {
                    using (FileStream savFile = new FileStream(arguments.InputFile, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader savFileReader = new BinaryReader(savFile))
                        {
                            savFileReader.BaseStream.Seek(0, SeekOrigin.Begin);
                            var s = new MemoryStream();
                            var header = savFileReader.ReadChars(4); // GRIM
                            var unknown = savFileReader.ReadInt32(); // ???
                            var uncompressedSizeData = savFileReader.ReadBytes(4);
                            var uncompressedSize = BitConverter.ToInt32(uncompressedSizeData, 0);
                            const int HeaderLength = 12;
                            var bytes = savFileReader.ReadBytes((int)savFileReader.BaseStream.Length - HeaderLength);
                            using (var m = new MemoryStream())
                            {
                                m.Write(Ionic.Zlib.ZlibStream.UncompressBuffer(bytes), 0, uncompressedSize);
                                m.Position = 0;
                                using (FileStream datFile = new FileStream(arguments.OutputFile, FileMode.Create))
                                {
                                    m.CopyTo(datFile);
                                    datFile.Flush();
                                    datFile.Close();
                                }
                            }
                        }
                    }
                }

                if (arguments.Operation == Operation.Pack)
                {
                    using (FileStream datFileStream = new FileStream(arguments.InputFile, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader datFileReader = new BinaryReader(datFileStream))
                        {
                            var bytes = datFileReader.ReadBytes((int)datFileReader.BaseStream.Length);
                            using (var m = new MemoryStream())
                            {
                                bytes = Ionic.Zlib.ZlibStream.CompressBuffer(bytes);
                                m.Write(bytes, 0, bytes.Length);
                                m.Position = 0;
                                using (FileStream savFile = new FileStream(arguments.OutputFile, FileMode.Create))
                                {
                                    savFile.WriteByte((byte)'G');
                                    savFile.WriteByte((byte)'R');
                                    savFile.WriteByte((byte)'I');
                                    savFile.WriteByte((byte)'M');
                                    savFile.Write(BitConverter.GetBytes((Int32)6), 0, 4); //TODO: document the '6'
                                    var uncompressSize = datFileReader.BaseStream.Length;
                                    var uncompressedSizeData = BitConverter.GetBytes((int)uncompressSize);
                                    savFile.Write(uncompressedSizeData, 0, uncompressedSizeData.Length);
                                    m.CopyTo(savFile);
                                    savFile.Flush();
                                    savFile.Close();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Arguments ParseArguments(string[] args)
        {
            var arguments = new Arguments();
            if (args.Length == 3)
            {
                // We require a known operation (pack/unpack)
                if (args[0] == "pack")
                {
                    arguments.Operation = Operation.Pack;
                }
                else if (args[0] == "unpack")
                {
                    arguments.Operation = Operation.Unpack;
                }
                else
                {
                    arguments.OperationError = true;
                }

                // We require the input file does exist
                if (File.Exists(args[1]))
                {
                    arguments.InputFile = args[1];
                }
                else
                {
                    arguments.InputFileError = true;
                }

                // We require the output file does not already exist
                if (!File.Exists(args[2]))
                {
                    arguments.OutputFile = args[2];
                }
                else
                {
                    arguments.OutputFileError = true;
                }
            }
            else
            {
                arguments.ArgumentCountError = true;
            }

            arguments.IsValid = !arguments.ArgumentCountError & !arguments.InputFileError & !arguments.OutputFileError & !arguments.OperationError;

            if (!arguments.IsValid)
            {
                Console.WriteLine("Invalid usage, expected usage patterns:");
                Console.WriteLine("  grSavTool unpack savFile datFile");
                Console.WriteLine("  grSavTool pack datFile savFile");
                Console.WriteLine("Note: The output file must not already exist");
            }

            return arguments;
        }

        private enum Operation
        {
            None,
            Pack,
            Unpack
        }

        private class Arguments
        {
            public Operation Operation;
            public string InputFile;
            public string OutputFile;
            public bool IsValid;
            public bool ArgumentCountError;
            public bool OperationError;
            public bool InputFileError;
            public bool OutputFileError;
        }
    }
}