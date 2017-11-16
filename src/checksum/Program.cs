﻿using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using checksum.infrastructure.commandline;
using checksum.infrastructure.configuration;

namespace checksum
{
    internal class Program
    {
        private enum HashLengthType
        {
            Md5 = 32,
            Sha1 = 40,
            Sha256 = 64,
            Sha512 = 128,
        }

        private const string MD5 = "md5";
        private const string SHA1 = "sha1";
        private const string SHA256 = "sha256";
        private const string SHA512 = "sha512";

        private static void Main(string[] args)
        {
            var configuration = new ConfigurationSettings();
            parse_arguments_and_set_up_configuration(configuration, args);

#if DEBUG
            Console.WriteLine("FilePath='{0}'", configuration.FilePath);
            Console.WriteLine("HashType='{0}'", configuration.HashType);
            Console.WriteLine("HashToCheck={0}", configuration.HashToCheck);
#endif

            configuration.FilePath = Path.GetFullPath(configuration.FilePath);
            if (!File.Exists(configuration.FilePath))
            {
                Console.WriteLine("File '{0}' doesn't exist.", configuration.FilePath);
                Environment.Exit(1);
            }

            if (!string.IsNullOrWhiteSpace(configuration.HashToCheck) && string.IsNullOrWhiteSpace(configuration.HashType))
            {
                var hashLength = configuration.HashToCheck.Length;
                switch (hashLength)
                {
                    case (int)HashLengthType.Md5:
                        configuration.HashType = MD5;
                        break;
                    case (int)HashLengthType.Sha1:
                        configuration.HashType = SHA1;
                        break;
                    case (int)HashLengthType.Sha256:
                        configuration.HashType = SHA256;
                        break;
                    case (int)HashLengthType.Sha512:
                        configuration.HashType = SHA512;
                        break;
                }
            }


            HashAlgorithm hash_util = new MD5CryptoServiceProvider();

            if (configuration.HashType.ToLowerSafe() == SHA1)
            {
                hash_util = new SHA1CryptoServiceProvider();
            }
            else if (configuration.HashType.ToLowerSafe() == SHA256)
            {
                hash_util = new SHA256CryptoServiceProvider();
            }
            else if (configuration.HashType.ToLowerSafe() == SHA512)
            {
                hash_util = new SHA512CryptoServiceProvider();
            }



            //todo: Wonder if we need to flip this for perf on very large files: http://stackoverflow.com/a/13926809
            var hash = hash_util.ComputeHash(File.Open(configuration.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            string hash_string = "";
            if (string.IsNullOrWhiteSpace(configuration.OutputType) || configuration.OutputType.ToLower() != "b64")
                hash_string = BitConverter.ToString(hash).Replace("-", string.Empty);
            else
                hash_string = Convert.ToBase64String(hash);




            if (string.IsNullOrWhiteSpace(configuration.HashToCheck))
            {
                Console.WriteLine(hash_string);
                pause_execution_if_debug();
                Environment.Exit(0);
            }

            var result = string.Compare(configuration.HashToCheck, hash_string, ignoreCase: true, culture: CultureInfo.InvariantCulture);

            if (result != 0)
            {
                Console.WriteLine("Error - hashes do not match. Actual value was '{0}'.", hash_string);
                Environment.ExitCode = 1;
            }
            else
            {
                Console.WriteLine("Hashes match.");
            }

            pause_execution_if_debug();
            Environment.Exit(Environment.ExitCode);
        }

        private static void pause_execution_if_debug()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue...");
            Console.ReadKey();
#endif
        }


        /// <summary>
        /// Parses the arguments and sets up the configuration
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="args">The arguments.</param>
        private static void parse_arguments_and_set_up_configuration(ConfigurationSettings configuration, string[] args)
        {
            bool help = false;

            OptionSet option_set = new OptionSet()
                .Add("?|help|h",
                     "Prints out the options.",
                     option => help = option != null)
                .Add("f=|file=",
                     "REQUIRED: file - The is the name of the file. The file should exist. You do not need to specify -f or -file in front of this argument.",
                     option => configuration.FilePath = option)
                .Add("t=|type=|hashtype=",
                     "Optional: hashtype - 'md5', 'sha1', 'sha256' or 'sha512'. Defaults to 'md5' or is determined by length of passed check value.",
                     option => configuration.HashType = option)
                 .Add("c=|check=",
                     "Optional: check - the signature you want to check. Not case sensitive.",
                     option => configuration.HashToCheck = option)
                 .Add("o=|output=",
                     "Optional:  output type - 'b64' for base64 string, 'hex' for hexadecimal string",
                     option => configuration.OutputType = option)
                ;

            try
            {
                var extra_args = option_set.Parse(args);
                if (extra_args != null && extra_args.Count != 0)
                {
                    if (string.IsNullOrWhiteSpace(configuration.FilePath))
                    {
                        configuration.FilePath = extra_args[0];
                    }
                }
            }
            catch (OptionException)
            {
                show_help(option_set);
            }
            if (help) show_help(option_set);
            if (string.IsNullOrWhiteSpace(configuration.FilePath)) show_help(option_set);


            configuration.FilePath = configuration.FilePath.Replace("'", string.Empty).Replace("\"", string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(configuration.HashType))
            {
                configuration.HashType = configuration.HashType.Trim();
            }
        }

        /// <summary>
        /// Shows the help menu and prints the options
        /// </summary>
        /// <param name="option_set">The option_set.</param>
        private static void show_help(OptionSet option_set)
        {
            Console.WriteLine("checksum - File CheckSum Validator - Apache v2");
            Console.WriteLine("checksum checks a file and returns a check sum for md5, sha1, sha256 and sha512.");
            Console.WriteLine("To use checksum you would simply provide a file path and it will return the sum for the file.");
            Console.WriteLine("  Example: checksum -f=\"a\\relative\\path\"");
            Console.WriteLine("  Example: checksum -f=\"a\\relative\\path\"");
            Console.WriteLine("  Example: checksum \"a\\relative\\path\" -t=sha256");
            Console.WriteLine("You can also check against an existing signature.");
            Console.WriteLine("To validate against an existing signature (hash) you would simply provide");
            Console.WriteLine(" the file and the expected signature. When checking a signature, if the ");
            Console.WriteLine(" signature is valid it exits with 0, otherwise it exits with a non-zero exit code.");
            Console.WriteLine("  Example: checksum -f=\"c:\\\\path\\to\\somefile.exe\" -c=\"thehash\"");
            Console.WriteLine("  Example: checksum \"c:\\\\path\\to\\somefile.exe\" -c=\"thehash\" -t=sha256");
            Console.WriteLine("");
            Console.WriteLine(" == Synopsis == ");
            Console.WriteLine("  checksum [-t=sha1|sha256|sha512|md5] [-c=signature] [-f=]filepath");
            Console.WriteLine("== Options ==");
            option_set.WriteOptionDescriptions(Console.Error);

            pause_execution_if_debug();
            Environment.Exit(-1);
        }

    }
}
