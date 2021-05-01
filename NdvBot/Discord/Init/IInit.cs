using System;
using System.Threading.Tasks;

namespace NdvBot.Discord.Init
{
    public interface IInit
    {
        public long Priority { get; set; }
        public Task Init(IServiceProvider serviceProvider);
        public Task DeInit();
    }
}