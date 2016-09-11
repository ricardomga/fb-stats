using System.Collections.Generic;

namespace Primavera.eCommerce.Statistics.Models
{
    public class FbUser
    {
        public FbUser()
        {
            Likes = new List<Like>();
            Comments = new List<Comment>();
        }
        public string FbUserId { get; set; }
        public string Name { get; set; }

        public virtual IList<Like> Likes { get; set; }
        public virtual IList<Comment> Comments { get; set; }
    }
}
