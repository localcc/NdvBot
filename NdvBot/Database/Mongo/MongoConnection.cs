using System.Data;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NdvBot.Config;

namespace NdvBot.Database.Mongo
{
    public class MongoConnection : IMongoConnection
    {
        public IMongoDatabase MainDb { get; }
        public IMongoDatabase ServerDb { get; }
        
        private const string MainDbName = "dev";
        
        public MongoConnection()
        {
            if (ConfigFile.Current is null)
            {
                throw new DataException("Config file not ready!");
            }
            var settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(ConfigFile.Current.DatabaseHost, ConfigFile.Current.DatabasePort),
                Credential = MongoCredential.CreateMongoX509Credential(ConfigFile.Current.DatabaseUsername),
                SslSettings = new SslSettings()
                {
                    ServerCertificateValidationCallback = CertificateValidationCallback,
                    ClientCertificates = new[]
                    {
                        new X509Certificate2(GetAuthFilePath(ConfigFile.Current.CertName),
                            ConfigFile.Current.CertPassword)
                    }
                },
                UseTls = true,
                AllowInsecureTls = false
            };
            
            var client = new MongoClient(settings);
            this.MainDb = client.GetDatabase(MainDbName);
            this.ServerDb = client.GetDatabase(ConfigFile.Current.DatabaseName);

            var conventionPack = new ConventionPack {new IgnoreExtraElementsConvention(true)};
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, t => true);
        }

        private static string GetAuthFilePath(string authFile)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "certs/" + authFile);
        }

        private static bool CertificateValidationCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (ConfigFile.Current is null)
            {
                throw new DataException("Config file not ready!");
            }
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                X509Certificate2 caCert = new X509Certificate2(GetAuthFilePath(ConfigFile.Current.CACertName));
                X509Chain caChain = new X509Chain
                {
                    ChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        RevocationFlag = X509RevocationFlag.EntireChain
                    }
                };
                caChain.ChainPolicy.ExtraStore.Add(caCert);

                X509Certificate2 sererCertificate = new X509Certificate2(cert);
                caChain.Build(sererCertificate);
                if (caChain.ChainStatus.Length == 0) return true;
                return caChain.ChainStatus.Any(s => s.Status == X509ChainStatusFlags.UntrustedRoot);
            }
            return false;
        }
    }
}