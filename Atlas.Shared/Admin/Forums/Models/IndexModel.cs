﻿using System;
using System.Collections.Generic;

namespace Atlas.Shared.Admin.Forums.Models
{
    public class IndexModel
    {
        public IList<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
        public IList<ForumModel> Forums { get; set; } = new List<ForumModel>();

        public class CategoryModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class ForumModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int SortOrder { get; set; }
            public int TotalTopics { get; set; }
            public int TotalReplies { get; set; }
            public string PermissionSetName { get; set; }
        }
    }
}
