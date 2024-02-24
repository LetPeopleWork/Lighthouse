using CMFTAspNet.Data;
using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
    {
        public FeatureRepository(Data.AppContext context) : base(context, (context) => context.Features)
        {            
        }
    }
}
