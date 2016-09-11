namespace Primavera.eCommerce.Statistics.Models
{
    public class Like
    {
        public int LikeId { get; set; }
        public string FbUserId { get; set; }
        public string ItemKey { get; set; }
        public string FbStoreId { get; set; }

        public virtual FbUser FbUser { get; set; }
        public virtual Item Item { get; set; }
    }
}