using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Day3Webhook
{
    public class SecretSantaImage : TableEntity
    {
        public string ImageName { get; set; }
        public string ImageUrl { get; set; }

        public SecretSantaImage(string name, string url)
        {
            ImageName = name;
            ImageUrl = url;
        }
    }
}
