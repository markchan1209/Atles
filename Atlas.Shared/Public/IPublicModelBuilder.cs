﻿using System;
using System.Threading.Tasks;
using Atlas.Shared.Public.Models;

namespace Atlas.Shared.Public
{
    public interface IPublicModelBuilder
    {
        Task<IndexPageModel> BuildIndexPageModelAsync(Guid siteId);
        Task<ForumPageModel> BuildForumPageModelAsync(Guid siteId, Guid forumId);
        Task<PostPageModel> BuildPostPageModelAsync(Guid siteId, Guid forumId);
        Task<TopicPageModel> BuildTopicPageModelAsync(Guid siteId, Guid forumId, Guid topicId);
    }
}
