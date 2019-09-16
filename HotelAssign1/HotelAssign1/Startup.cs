using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HotelAssign1.Startup))]
namespace HotelAssign1
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
