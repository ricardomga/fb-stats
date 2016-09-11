namespace Primavera.eCommerce.Statistics.Models
{
    public class Comment
    {
        public string CommentId { get; set; }
        public string Message { get; set; }
        public string CreatedTime { get; set; }
        public string FbUserId { get; set; }
        public string ItemKey { get; set; }
        public string FbStoreId { get; set; }

        public virtual FbUser FbUser { get; set; }
        public virtual Item Item { get; set; }
    }
}
