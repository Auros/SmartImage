using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmartImage.Tests
{
    public static class TestHelpers
    {
        public const string AurosGitHubProfilePictureUrl = "https://avatars.githubusercontent.com/u/41306347?v=4";
        
        public static SmartImageManager GetSIM()
        {
            var simgo = Object.Instantiate(Resources.Load<GameObject>("Smart Image Manager"));
            return simgo!.GetComponent<SmartImageManager>();
        }
    }
}
