﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Sequencer.Configuration.Model;

namespace Sequencer.Configuration
{
    class PasswordSequenceConfigurationFactory
    {
        public PasswordSequenceConfiguration LoadFromResource(string resource)
        {
            if (resource == null)
                throw new Exception("resource must be a valid file resource");

            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    sw.Write(resource);
                    stream.Seek(0, SeekOrigin.Begin);

                    return LoadFromStream(stream);
                }
            }

        }

        private System.Configuration.Configuration sequencerConfiguration;
        public System.Configuration.Configuration SequencerConfiguration
        {
            get
            {
                /* Getting config path from a mashup of:
                 *  http://stackoverflow.com/a/5191101/1390430
                 *  http://stackoverflow.com/a/2272628/1390430
                 */
                return sequencerConfiguration ?? (sequencerConfiguration =
                                                  System.Configuration.ConfigurationManager.OpenExeConfiguration(typeof(Sequencer).Assembly.Location));
            }
        }

        public PasswordSequenceConfiguration LoadFromUserFile(string profileName = null)
        {
            string userFilePath = GetUserFilePath(profileName);
            if (userFilePath != null && File.Exists(userFilePath))
                return LoadFromFile(userFilePath);

            return null;
        }

        public string GetUserFilePath(string profileName = null)
        {
            string config = null;
            if (SequencerConfiguration.AppSettings.Settings["userConfigPath"] != null)
                config = SequencerConfiguration.AppSettings.Settings["userConfigPath"].Value;

            if (null != config)
            {
                config = InsertProfileNameInPath(config, profileName);
                if (!System.IO.Path.IsPathRooted(config))
                {
                    config = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        config);
                }
            }

            if (null == config)
            {
                config = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "sequencer.xml");

                config = InsertProfileNameInPath(config, profileName);
            }

            if (null != config)
            {
                return System.IO.Path.GetFullPath(config);
            }
            else
            {
                return null; /* TODO: better to throw exception? */
            }

        }

        public PasswordSequenceConfiguration LoadFromSystemFile(string profileName = null)
        {
            string config = null;
            if (SequencerConfiguration.AppSettings.Settings["defaultConfigPath"] != null)
            {
                config = SequencerConfiguration.AppSettings.Settings["defaultConfigPath"].Value;
            }
            if (null == config && SequencerConfiguration.AppSettings.Settings["configPath"] != null)
            {
                config = SequencerConfiguration.AppSettings.Settings["configPath"].Value;
            }

            config = InsertProfileNameInPath(config, profileName);

            if (null != config && !File.Exists(config))
            {
                return LoadFromFile(System.IO.Path.GetFullPath(config));
            }
            else
            {
                return null; /* TODO: better to throw exception? */
            }

        }

        public ICollection<string> ListConfigurationFiles()
        {
            string path = GetUserFilePath();
            if (Directory.Exists(Path.GetDirectoryName(path)))
                return Directory.GetFiles(Path.GetDirectoryName(path), string.Format("{0}*{1}", Path.GetFileNameWithoutExtension(path), Path.GetExtension(path)));
            return new List<string>();
        }

        public PasswordSequenceConfiguration LoadFromFile(string path)
        {
            if (path == null || path == string.Empty)
                throw new Exception("path must be a valid file path");

            PasswordSequenceConfiguration config = null;
            try
            {
                if (File.Exists(path))
                {
                    using (FileStream configStream = File.OpenRead(path))
                    {
                        config = LoadFromStream(configStream);
                    }
                }
                else
                {
                    /* Config file not found; create empty config */
                    config = new PasswordSequenceConfiguration(true);
                    /* TODO: pop up an error message or something? */
                }
            }
            catch (InvalidOperationException)
            {
                config = null;
            }
            return config;
        }

        private PasswordSequenceConfiguration LoadFromStream(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PasswordSequenceConfiguration),
                                                         "http://quasivirtuel.com/PasswordSequenceConfiguration.xsd");

            return (PasswordSequenceConfiguration)serializer.Deserialize(XmlReader.Create(stream));
        }

        private string InsertProfileNameInPath(string path, string profileName)
        {
            string config = path;
            if (!string.IsNullOrEmpty(profileName))
            {
                string extension = Path.GetExtension(config);
                config = config.TrimEnd(extension.ToCharArray());
                config = string.Format("{0}.{1}{2}", config, profileName, extension);
            }
            return config;
        }



    }
}
