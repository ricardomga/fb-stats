using Facebook;
using Newtonsoft.Json;
using Primavera.eCommerce.Statistics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Primavera.eCommerce.Statistics.Controllers
{
    public class StatisticsController : Controller
    {

        #region PrivateFields

        private List<Item> _items;
        private StatisticsContext _statisticsContext;

        #endregion


        #region Constructor

        public StatisticsController()
        {
            _items = new List<Item>();
            _statisticsContext = new StatisticsContext();
        }

        #endregion


        #region PublicMethods

        // GET: Statistics
        [HttpGet]
        public ActionResult FbInfo()
        {

            return View();
        }

        [HttpPost]
        public ActionResult StoreStats(string accessToken, string fbStoreId)
        {
            ViewBag.AccessToken = accessToken;
            ViewBag.FbStoreId = fbStoreId;

            return View();
        }

        [HttpPost]
        public ActionResult UpdateStats(string accessToken, string fbStoreId)
        {
            // this url is comon to all products in the store that made the request
            var baseUrl = "https://socialbs.azurewebsites.net/" + fbStoreId + "/products/";

            try
            {
                _items = GetItemsFromBS(fbStoreId).Result;

                // get the urls of all the items to make the fb request
                var urls = GetItemsUrls(baseUrl);

                dynamic fbResponseData = GetDataFromFb(accessToken, urls);

                SaveFbStats(fbResponseData, baseUrl, fbStoreId);
            }
            catch (Exception)
            {
                throw new Exception("Error updating the statistics");
            }
            finally
            {
                _statisticsContext.Dispose();
            }

            ViewBag.AccessToken = accessToken;
            ViewBag.FbStoreId = fbStoreId;

            return View("StoreStats");
        }


        /// <summary>
        /// Loads the table data.
        /// </summary>
        /// <param name="fbStoreId">The fb store identifier.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetGeneralStats(string fbStoreId)
        {
            _items = _statisticsContext.Items.ToList();

            var dataQuery = from i in _statisticsContext.Items
                            where i.FbStoreId == fbStoreId
                            select new
                            {
                                ItemKey = i.ItemKey,
                                ItemDescription = i.Description,
                                NumLikes = i.Likes.Count,
                                NumComments = i.CommentCount,
                                NumShares = i.ShareCount
                            };

            var data = dataQuery.ToList();

            return Json(new { data = data });
        }

        [HttpPost]
        public string ItemData(string fbStoreId, string itemKey)
        {
            var item = GetItem(fbStoreId, itemKey);

            var jsonItem = JsonConvert.SerializeObject(item, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            return jsonItem;
        }

        private Item GetItem(string fbStoreId, string itemKey)
        {
            var itemQuery = from i in _statisticsContext.Items
                            where i.ItemKey == itemKey && i.FbStoreId == fbStoreId
                            select i;

            var item = itemQuery.FirstOrDefault();
            return item;
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Gets the data from fb.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="urls">The urls.</param>
        /// <returns></returns>
        private dynamic GetDataFromFb(string accessToken, string urls)
        {
            var fb = new FacebookClient(accessToken) { AppId = "1557527997889365" };

            // facebook batch to make all several requests at the same time
            var result = fb.Batch(new FacebookBatchParameter
            {
                Data = new { name = "get-id", omit_response_on_success = false },
                Parameters = new { ids = urls, fields = "og_object{id}, share" }
            }, new FacebookBatchParameter
            {
                // this will make a request for every og_id recived from the above request through JsonPath
                // some information about JsonPath http://goessner.net/articles/JsonPath/
                Parameters = new { ids = "{result=get-id:$.*.og_object.id}", fields = "likes, comments" }
            });

            return (IList<object>)result;
        }


        /// <summary>
        /// Saves the fb information.
        /// </summary>
        /// <param name="fbResponseData">The fb response data.</param>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="fbStoreId">The fb page identifier.</param>
        private void SaveFbStats(dynamic fbResponseData, string baseUrl, string fbStoreId)
        {
            if (fbResponseData[0] is Exception || fbResponseData[1] is Exception)
            {
                throw new Exception("fb request failed");
            }

            foreach (var item in _items)
            {
                dynamic share = fbResponseData[0][baseUrl + item.ItemKey].share;
                SaveItem(item, fbStoreId, share.share_count, share.comment_count);

                // og(open graph) object id is the id thar facebook gives to the url of the product
                // with this url we can make more specific requests such as likes and comments
                string ogObjectId = fbResponseData[0][baseUrl + item.ItemKey].og_object.id;

                // fbResponseData[1] is the result from the og_ids request and 
                //contains  the comments and likes of every products
                dynamic likes = fbResponseData[1][ogObjectId].likes;
                SaveItemLikes(item, likes, fbStoreId);

                dynamic comments = fbResponseData[1][ogObjectId].comments;
                SaveItemComments(item, comments, fbStoreId);
            }

            // save the changes to the db
            _statisticsContext.SaveChanges();
        }

        private void SaveItem(Item item, string fbStoreId, long shareCount, long commentCount)
        {
            if (ItemExists(item.ItemKey, fbStoreId))
                return;
            item.FbStoreId = fbStoreId;
            // fbResponseData[0] is the result from the url requests and
            //contains the comment count and share count for every products in the share object 
            item.ShareCount = shareCount;
            item.CommentCount = commentCount;


            // add item to the db
            _statisticsContext.Items.Add(item);
        }

        private bool ItemExists(string itemItemKey, string fbStoreId)
        {
            return GetItem(fbStoreId, itemItemKey) != null;
        }

        private void SaveItemComments(Item item, dynamic comments, string fbStoreId)
        {
            // check if product has any likes
            if (comments == null) return;

            // add comments to the db
            foreach (var comment in comments.data)
            {
                if (!CommentExists(comment))
                {
                    CreateUserIfNotExists(comment.@from.id, comment.@from.name);
                    _statisticsContext.Comments.Add(new Comment
                    {
                        CommentId = comment.id,
                        CreatedTime = comment.created_time,
                        Message = comment.message,
                        FbStoreId = fbStoreId,
                        FbUserId = comment.@from.id,
                        ItemKey = item.ItemKey
                    });
                }
            }
        }

        private bool CommentExists(dynamic fbComment)
        {
            string commentId = fbComment.id;
            var commentQuery = from c in _statisticsContext.Comments
                               where c.CommentId == commentId
                               select c;

            var comment = commentQuery.FirstOrDefault();

            var exists = comment != null;

            return exists;
        }

        private void SaveItemLikes(Item item, dynamic likes, string fbStoreId)
        {
            // check if product has any likes
            if (likes == null) return;

            // add likes to the db
            foreach (var like in likes.data)
            {
                if (!LikeExists(like, item.ItemKey, fbStoreId))
                {
                    CreateUserIfNotExists(like.id, like.name);
                    _statisticsContext.Likes.Add(new Like { FbUserId = like.id, ItemKey = item.ItemKey, FbStoreId = fbStoreId });
                }
            }
        }

        private bool LikeExists(dynamic fbLike, string itemKey, string fbStoreId)
        {
            var itemQuery = from i in _statisticsContext.Items
                            where i.ItemKey == itemKey && i.FbStoreId == fbStoreId
                            select i;

            var item = itemQuery.FirstOrDefault();

            var exists = false;

            if (item != null)
            {
                foreach (var like in item.Likes)
                {
                    if (like.FbUserId == fbLike.id)
                    {
                        exists = true;
                        break;
                    }

                }
            }

            return exists;
        }


        /// <summary>
        /// Creates the user if not exists.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        private void CreateUserIfNotExists(string id, string name)
        {
            if (_statisticsContext.FbUsers.Find(id) == null)
            {
                _statisticsContext.FbUsers.Add(new FbUser { FbUserId = id, Name = name });
            }
        }





        /// <summary>
        /// Gets the items urls.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <returns></returns>
        private string GetItemsUrls(string baseUrl)
        {
            var urls = "";

            foreach (var item in _items)
            {
                urls += baseUrl + item.ItemKey + ",";
            }

            // return the urls separated by ',' without the ',' in the end
            return urls.Remove(urls.Length - 1);
        }



        /// <summary>
        /// Gets the items from bs.
        /// </summary>
        /// <param name="fbStoreId">The fb page identifier.</param>
        /// <returns></returns>
        private async Task<List<Item>> GetItemsFromBS(string fbStoreId)
        {
            List<Item> items = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://socialbs.azurewebsites.net/api/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getItemsMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "items?fbStoreId=" + fbStoreId);

                var response = client.SendAsync(getItemsMessage);

                if (response.Result.IsSuccessStatusCode)
                {
                    items = await response.Result.Content.ReadAsAsync<List<Item>>();
                }
                else
                {
                    throw new Exception("request failed");
                }
            }
            return items;
        }


        #endregion

    }
}