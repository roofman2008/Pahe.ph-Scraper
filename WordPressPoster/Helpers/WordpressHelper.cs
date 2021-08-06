using System;
using System.Runtime.InteropServices;
using WordPressPoster.Models;
using WordPressRestApi;
using WordPressRestApi.CreateModel;
using WordPressRestApi.Models;
using WordPressRestApi.QueryModel;
using WordPressRestApi.UpdateModel;

namespace WordPressPoster.Helpers
{
    public class WordpressHelper
    {
        private const string Username = "roofman";
        private const string Token = "lgqU hTTI DiwF 5ao6 3L0h DMWv";
        private static readonly WordPressApiClient Client = new WordPressApiClient("http://localhost:9000/wordpress/wp-json/wp/v2");

        public static Post CreatePost(PostCreate post)
        {
            var result = Client.CreatePost(
                new AuthenticationTokens() { ApplicationPassword = Token, UserName = Username }, post).Result;
            return result;
        }

        public static Post GetPost(int postId)
        {
            var result = Client.GetPost(new PostQuery(), postId).Result;
            return result;
        }

        public static Post UpdatePost(int postId, PostUpdate post)
        {
            var result = Client
                .UpdatePost(new AuthenticationTokens() {ApplicationPassword = Token, UserName = Username}, post, postId)
                .Result;
            return result;
        }
    }
}