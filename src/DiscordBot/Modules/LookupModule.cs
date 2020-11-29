﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Kgsearch.v1.Data;
using MariBot.Models.Google.KnowledgeGraph;
using MariBot.Services;
using Newtonsoft.Json;
using UrbanDictionnet;
using static MariBot.Services.WikipediaService;

namespace MariBot.Modules
{
    /*
     * Collection of commands for looking up things like definitions.
     */
    public class LookupModule : ModuleBase<SocketCommandContext>
    {
        public GoogleService GoogleService { get; set; }
        public PictureService PictureService { get; set; }
        public UrbanDictionaryService UrbanDictionaryService { get; set; }
        public WikipediaService WikipediaService { get; set; }

        [Command("urban")]
        public Task urban([Remainder] string word)
        {
            DefinitionData result;
            try
            {
                result = UrbanDictionaryService.GetRandomDefinition(word).Result;
            }
            catch (AggregateException ex)
            {
                return ReplyAsync(ex.InnerException.Message);
            }
            string output ="";
            result.Definition = result.Definition.Replace("[", "");
            result.Definition = result.Definition.Replace("]", "");
            result.Example = result.Example.Replace("[", "");
            result.Example = result.Example.Replace("]", "");
            output += "**" + result.Word + "'s definition**\n\n";
            output += result.Definition + "\n\n";
            output += "**Example**\n\n";
            output += result.Example + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("urbanrand")]
        public Task urbanrand()
        {
            var result = UrbanDictionaryService.GetRandomWord().Result;
            string output = "";
            result.Definition = result.Definition.Replace("[", "");
            result.Definition = result.Definition.Replace("]", "");
            result.Example = result.Example.Replace("[", "");
            result.Example = result.Example.Replace("]", "");
            output += "**" + result.Word + "'s definition**\n\n";
            output += result.Definition + "\n\n";
            output += "**Example**\n\n";
            output += result.Example + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("urbantop")]
        public Task urbantop([Remainder] string word)
        {
            DefinitionData result;
            try
            {
                result = UrbanDictionaryService.GetTopDefinition(word).Result;
            }
            catch (AggregateException ex)
            {
                return ReplyAsync(ex.InnerException.Message);
            }

            string output = "";
            result.Definition = result.Definition.Replace("[", "");
            result.Definition = result.Definition.Replace("]", "");
            result.Example = result.Example.Replace("[", "");
            result.Example = result.Example.Replace("]", "");
            output += "**" + result.Word + "'s definition**\n\n";
            output += result.Definition + "\n\n";
            output += "**Example**\n\n";
            output += result.Example + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("wiki")]
        public Task wikipedia([Remainder] string topic)
        {
            var state = Context.Channel.EnterTypingState();
            WikipediaObject result;

            try
            {
                result = WikipediaService.GetWikipediaPage(topic).Result;
            } catch (Exception e)
            {
                state.Dispose();
                return Context.Channel.SendMessageAsync(e.Message);
            }

            string output = result.text;
            output = trimToLength(output, 2048);

            var eb = new EmbedBuilder();
            eb.WithTitle(result.title);
            eb.WithDescription(output);
            eb.WithColor(Color.Blue);
            eb.WithUrl(result.link);

            if (result.imageURL != null)
            {
                eb.WithImageUrl(result.imageURL);
            }

            state.Dispose();
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("wikisearch")]
        public Task wikipediaSearch([Remainder] string topic)
        {
            var state = Context.Channel.EnterTypingState();
            var result = WikipediaService.GetWikipediaResults(topic).Result;
            string output = "";
            for (int i = 0; i < result.Count; i++)
            {
                output += result[i] + "\n";
            }
            var eb = new EmbedBuilder();
            eb.WithTitle("Results for " + topic);
            eb.WithDescription(output);
            eb.WithColor(Color.Blue);
            state.Dispose();
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("google", RunMode = RunMode.Async)]
        public async Task google([Remainder] string keywords)
        {
            Search searchResults = GoogleService.Search(keywords).Result;
            var eb = new EmbedBuilder();
            if (searchResults.Items.Count < 1)
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Red);
                eb.WithDescription("No results were found.");
            } else
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Green);
                string description = "";
                foreach(Result result in searchResults.Items)
                {
                    string formattedResult = ConvertSearchToMessage(result);
                    if((description + formattedResult).Length <= 2048)
                    {
                        description += formattedResult;
                    }
                }
                eb.WithDescription(description);
            }
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        [Command("kg", RunMode = RunMode.Async)]
        public async Task knowledgegraph([Remainder] string keywords)
        {
            SearchResponse searchResponse = GoogleService.KnowledgeGraph(keywords).Result;
            var eb = new EmbedBuilder();
            if (searchResponse.ItemListElement.Count < 1)
            {
                eb.WithTitle("Google Knowledge Graph");
                eb.WithColor(Color.Red);
                eb.WithDescription("No result was found.");
            }
            else
            {
                eb.WithAuthor("Google Knowledge Graph");
                eb.WithColor(Color.Green);

                EntitySearchResult result = JsonConvert.DeserializeObject<EntitySearchResult>(searchResponse.ItemListElement[0].ToString());
                eb.WithTitle(result.Result.Name);
                eb.WithDescription(ConvertKnowledgeGraphEntityToMessage(result.Result));
                eb.WithUrl(result.Result.DetailedDescription.Url);
                if(result.Result.Image != null)
                eb.WithThumbnailUrl(result.Result.Image.ContentUrl);
            }
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        [Command("image", RunMode = RunMode.Async)]
        public async Task googleimage([Remainder] string keywords)
        {
            Search searchResults = GoogleService.Search(keywords, true).Result;
            var eb = new EmbedBuilder();
            if (searchResults.Items.Count < 1)
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Red);
                eb.WithDescription("No results were found.");
            }
            else
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Green);
                string description = "";
                Result result = searchResults.Items[0];
                eb.WithImageUrl(result.Link);
            }
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        private String trimToLength(String text, int maxLength)
        {
            if(maxLength < 0)
            {
                throw new ArgumentException("Invalid argument maxLength");
            }

            if(maxLength > text.Length)
            {
                return text;
            }

            int totalToRemove = text.Length - maxLength;

            return text.Remove(maxLength, totalToRemove);
        }

        private string ConvertSearchToMessage(Result result)
        {
            return "**" + result.Title + "**\n" + result.Link + "\n";
        }

        private string ConvertKnowledgeGraphEntityToMessage(Entity entity)
        {
            return "*" + entity.Description + "*\n\n" + entity.DetailedDescription.ArticleBody; 
        }
    }
}
