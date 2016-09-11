using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Primavera.eCommerce.Statistics.Models
{
    public class Item
    {
        public Item()
        {
            Likes = new List<Like>();
            Comments = new List<Comment>();
        }

        [Key]
        [Column(Order = 1)]
        public string ItemKey { get; set; }
        [Key]
        [Column(Order = 2)]
        public string FbStoreId { get; set; }
        public string Description { get; set; }
        public long ShareCount { get; set; }
        public long CommentCount { get; set; }

        public virtual IList<Like> Likes { get; set; }
        public virtual IList<Comment> Comments { get; set; }


    }

}
