﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Resources;
using Sitecore.Resources.Media;
using Sitecore.SharedModules.ImageCropping.Resources.Media;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.Dialogs.MediaBrowser;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.SharedModules.ImageCropping.Data.Fields
{
    /// <summary>
    /// Represents an Image field.
    /// 
    /// </summary>
    public class ExtendedImageField : Image
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.ContentEditor.Image"/> class.
        /// 
        /// </summary>
        public ExtendedImageField() : base()
        {
        }

        /// <summary>
        /// Renders the control.
        /// 
        /// </summary>
        /// <param name="output">The output.</param>
        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull((object)output, "output");
            Item mediaItem = this.GetMediaItem();
            string src;
            this.GetSrc(out src);
            string str1 = " src=\"" + src + "\"";
            string str2 = " id=\"" + this.ID + "_image\"";
            string str3 = " alt=\"" + (mediaItem != null ? WebUtil.HtmlEncode(mediaItem["Alt"]) : string.Empty) + "\"";
            //base.DoRender(output);
            output.Write("<div id=\"" + this.ID + "_pane\" class=\"scContentControlImagePane\">");
            string clientEvent = Sitecore.Context.ClientPage.GetClientEvent(this.ID + ".Browse");
            output.Write("<div class=\"scContentControlImageImage\" onclick=\"" + clientEvent + "\">");
            output.Write("<iframe" + str2 + str1 + str3 + " frameborder=\"0\" marginwidth=\"0\" marginheight=\"0\" width=\"100%\" height=\"128\" allowtransparency=\"allowtransparency\"></iframe>");
            output.Write("</div>");
            output.Write("<div id=\"" + this.ID + "_details\" class=\"scContentControlImageDetails\">");
            string details = this.GetDetails();
            output.Write(details);
            output.Write("</div>");
            output.Write("</div>");
        }

        /// <summary>
        /// Shows the properties.
        /// 
        /// </summary>
        /// <param name="args">The args.</param>
        protected new void ShowProperties(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            if (this.Disabled)
                return;
            string attribute = this.XmlValue.GetAttribute("mediaid");
            if (string.IsNullOrEmpty(attribute))
                SheerResponse.Alert("Select an image from the Media Library first.", new string[0]);
            else if (args.IsPostBack)
            {
                if (!args.HasResult)
                    return;
                this.XmlValue = new XmlValue(args.Result, "image");
                this.Value = this.XmlValue.GetAttribute("mediapath");
                this.SetModified();
                this.Update();
            }
            else
            {
                UrlString urlString = new UrlString("/sitecore/shell/~/xaml/Sitecore.Shell.Applications.Media.ExtendedImageProperties.aspx");
                Item obj = Client.ContentDatabase.GetItem(attribute, Language.Parse(this.ItemLanguage));
                if (obj == null)
                {
                    SheerResponse.Alert("Select an image from the Media Library first.", new string[0]);
                }
                else
                {
                    obj.Uri.AddToUrlString(urlString);
                    UrlHandle urlHandle = new UrlHandle();
                    urlHandle["xmlvalue"] = this.XmlValue.ToString();
                    urlHandle.Add(urlString);
                    SheerResponse.ShowModalDialog(urlString.ToString(), "700", "700", string.Empty, true);
                    args.WaitForPostBack();
                }
            }
        }

        /// <summary>
        /// Gets the image source.
        /// 
        /// </summary>
        /// <param name="src">The image source.</param>
        private void GetSrc(out string src)
        {
            src = string.Empty;
            MediaItem mediaItem = (MediaItem)this.GetMediaItem();
            if (mediaItem == null)
                return;
            CustomMediaUrlOptions thumbnailOptions = CustomMediaUrlOptions.GetThumbnailOptions(mediaItem);
            int result;
            if (!int.TryParse(mediaItem.InnerItem["Height"], out result))
                result = 128;
            thumbnailOptions.Height = Math.Min(128, result);
            thumbnailOptions.MaxWidth = 640;
            thumbnailOptions.UseDefaultIcon = true;

            XmlValue xmlValue = this.XmlValue;
            string cropRegion = WebUtil.HtmlEncode(xmlValue.GetAttribute("cropregion"));
            if (!string.IsNullOrEmpty(cropRegion))
            {
                thumbnailOptions.CropRegion = cropRegion;
            }
            src = CustomMediaManager.GetMediaUrl(mediaItem, thumbnailOptions);
        }

        /// <summary>
        /// Gets the details.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The details.
        /// </returns>
        /// <contract><ensures condition="not null"/></contract>
        private string GetDetails()
        {
            string str1 = string.Empty;
            MediaItem mediaItem = (MediaItem)this.GetMediaItem();
            if (mediaItem != null)
            {
                Item innerItem = mediaItem.InnerItem;
                StringBuilder stringBuilder = new StringBuilder();
                XmlValue xmlValue = this.XmlValue;
                stringBuilder.Append("<div>");
                string str2 = innerItem["Dimensions"];
                string str3 = WebUtil.HtmlEncode(xmlValue.GetAttribute("width"));
                string str4 = WebUtil.HtmlEncode(xmlValue.GetAttribute("height"));
                string cropRegion = WebUtil.HtmlEncode(xmlValue.GetAttribute("cropregion"));
                if (!string.IsNullOrEmpty(str3) || !string.IsNullOrEmpty(str4))
                {
                    stringBuilder.Append(Translate.Text("Dimensions: {0} x {1} (Original: {2})", (object)str3, (object)str4, (object)str2));
                }
                else
                {
                    stringBuilder.Append(Translate.Text("Dimensions: {0}", new object[1]
                      {
                        (object) str2
                      }));
                }
                stringBuilder.Append("</div>");
                stringBuilder.Append("<div style=\"padding:2px 0px 0px 0px\">");
                string str5 = WebUtil.HtmlEncode(innerItem["Alt"]);
                string str6 = WebUtil.HtmlEncode(xmlValue.GetAttribute("alt"));
                if (!string.IsNullOrEmpty(str6) && !string.IsNullOrEmpty(str5))
                    stringBuilder.Append(Translate.Text("Alternate Text: \"{0}\" (Default Alternate Text: \"{1}\")", (object)str6, (object)str5));
                else if (!string.IsNullOrEmpty(str6))
                    stringBuilder.Append(Translate.Text("Alternate Text: \"{0}\"", new object[1]
          {
            (object) str6
          }));
                else if (!string.IsNullOrEmpty(str5))
                    stringBuilder.Append(Translate.Text("Default Alternate Text: \"{0}\"", new object[1]
          {
            (object) str5
          }));
                else
                    stringBuilder.Append(Translate.Text("Warning: Alternate Text is missing."));
                stringBuilder.Append("</div>");
                str1 = ((object)stringBuilder).ToString();
            }
            if (str1.Length == 0)
                str1 = Translate.Text("This media item has no details.");
            return str1;
        }

        /// <summary>
        /// Gets the media item.
        /// </summary>  
        /// <returns>
        /// The media item.
        /// </returns>
        private Item GetMediaItem()
        {
            string attribute = this.XmlValue.GetAttribute("mediaid");
            if (attribute.Length <= 0)
                return (Item)null;
            Language language = Language.Parse(this.ItemLanguage);
            return Client.ContentDatabase.GetItem(attribute, language);
        }
     }
}