﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Atlify.Data.Caching;
using Atlify.Data.Services;
using Atlify.Domain;
using Atlify.Domain.Categories;
using Atlify.Domain.Forums;
using Atlify.Domain.Members;
using Atlify.Domain.Posts;
using Atlify.Domain.Posts.Commands;
using AutoFixture;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Atlify.Data.Tests.Services
{
    [TestFixture]
    public class TopicServiceTests : TestFixtureBase
    {
        [Test]
        public async Task Should_create_new_topic_and_add_event()
        {
            var options = Shared.CreateContextOptions();

            var categoryId = Guid.NewGuid();
            var forumId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var category = new Category(categoryId, Guid.NewGuid(), "Category", 1, Guid.NewGuid());
            var forum = new Forum(forumId, category.Id, "Forum", "my-forum", "My Forum", 1);
            var member = new Member(memberId, Guid.NewGuid().ToString(), "Email", "Display Name");

            using (var dbContext = new AtlifyDbContext(options))
            {
                dbContext.Categories.Add(category);
                dbContext.Forums.Add(forum);
                dbContext.Members.Add(member);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlifyDbContext(options))
            {
                var command = Fixture.Build<CreateTopic>()
                        .With(x => x.ForumId, forum.Id)
                        .With(x => x.MemberId, memberId)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();

                var createValidator = new Mock<IValidator<CreateTopic>>();
                createValidator
                    .Setup(x => x.ValidateAsync(command, new CancellationToken()))
                    .ReturnsAsync(new ValidationResult());

                var updateValidator = new Mock<IValidator<UpdateTopic>>();

                var sut = new TopicService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.CreateAsync(command);

                var topic = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == command.Id);
                var @event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == command.Id);

                var updatedCategory = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
                var updatedForum = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum.Id);
                var updatedMember = await dbContext.Members.FirstOrDefaultAsync(x => x.Id == memberId);

                createValidator.Verify(x => x.ValidateAsync(command, new CancellationToken()));
                Assert.NotNull(topic);
                Assert.NotNull(@event);
                Assert.AreEqual(category.TopicsCount + 1, updatedCategory.TopicsCount);
                Assert.AreEqual(topic.Id, updatedForum.LastPostId);
                Assert.AreEqual(forum.TopicsCount + 1, updatedForum.TopicsCount);
                Assert.AreEqual(forum.TopicsCount + 1, updatedForum.TopicsCount);
                Assert.AreEqual(member.TopicsCount + 1, updatedMember.TopicsCount);
            }
        }

        [Test]
        public async Task Should_update_topic_and_add_event()
        {
            var options = Shared.CreateContextOptions();

            var siteId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var forumId = Guid.NewGuid();

            var category = new Category(categoryId, siteId, "Category", 1, Guid.NewGuid());
            var forum = new Forum(forumId, category.Id, "Forum", "my-forum", "My Forum", 1);
            var topic = Post.CreateTopic(forumId, Guid.NewGuid(), "Title", "slug", "Content", StatusType.Published);

            using (var dbContext = new AtlifyDbContext(options))
            {
                dbContext.Categories.Add(category);
                dbContext.Forums.Add(forum);
                dbContext.Posts.Add(topic);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlifyDbContext(options))
            {
                var command = Fixture.Build<UpdateTopic>()
                    .With(x => x.Id, topic.Id)
                    .With(x => x.ForumId, forumId)
                    .With(x => x.SiteId, siteId)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();

                var createValidator = new Mock<IValidator<CreateTopic>>();

                var updateValidator = new Mock<IValidator<UpdateTopic>>();
                updateValidator
                    .Setup(x => x.ValidateAsync(command, new CancellationToken()))
                    .ReturnsAsync(new ValidationResult());

                var sut = new TopicService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.UpdateAsync(command);

                var updatedTopic = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == command.Id);
                var @event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == command.Id);

                updateValidator.Verify(x => x.ValidateAsync(command, new CancellationToken()));
                Assert.AreEqual(command.Title, updatedTopic.Title);
                Assert.NotNull(@event);
            }
        }

        [Test]
        public async Task Should_pin_topic_and_add_event()
        {
            var options = Shared.CreateContextOptions();

            var siteId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var forumId = Guid.NewGuid();

            var category = new Category(categoryId, siteId, "Category", 1, Guid.NewGuid());
            var forum = new Forum(forumId, category.Id, "Forum", "my-forum", "My Forum", 1);
            var topic = Post.CreateTopic(forumId, Guid.NewGuid(), "Title", "slug", "Content", StatusType.Published);

            using (var dbContext = new AtlifyDbContext(options))
            {
                dbContext.Categories.Add(category);
                dbContext.Forums.Add(forum);
                dbContext.Posts.Add(topic);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlifyDbContext(options))
            {
                var command = Fixture.Build<PinTopic>()
                    .With(x => x.Id, topic.Id)
                    .With(x => x.ForumId, forumId)
                    .With(x => x.SiteId, siteId)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();
                var createValidator = new Mock<IValidator<CreateTopic>>();
                var updateValidator = new Mock<IValidator<UpdateTopic>>();

                var sut = new TopicService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.PinAsync(command);

                var pinnedTopic = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == command.Id);
                var @event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == command.Id);

                Assert.AreEqual(command.Pinned, pinnedTopic.Pinned);
                Assert.NotNull(@event);
            }
        }

        [Test]
        public async Task Should_lock_topic_and_add_event()
        {
            var options = Shared.CreateContextOptions();

            var siteId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var forumId = Guid.NewGuid();

            var category = new Category(categoryId, siteId, "Category", 1, Guid.NewGuid());
            var forum = new Forum(forumId, category.Id, "Forum", "my-forum", "My Forum", 1);
            var topic = Post.CreateTopic(forumId, Guid.NewGuid(), "Title", "slug", "Content", StatusType.Published);

            using (var dbContext = new AtlifyDbContext(options))
            {
                dbContext.Categories.Add(category);
                dbContext.Forums.Add(forum);
                dbContext.Posts.Add(topic);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlifyDbContext(options))
            {
                var command = Fixture.Build<LockTopic>()
                    .With(x => x.Id, topic.Id)
                    .With(x => x.ForumId, forumId)
                    .With(x => x.SiteId, siteId)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();
                var createValidator = new Mock<IValidator<CreateTopic>>();
                var updateValidator = new Mock<IValidator<UpdateTopic>>();

                var sut = new TopicService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.LockAsync(command);

                var lockedTopic = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == command.Id);
                var @event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == command.Id);

                Assert.AreEqual(command.Locked, lockedTopic.Locked);
                Assert.NotNull(@event);
            }
        }

        [Test]
        public async Task Should_delete_topic_and_add_event()
        {
            var options = Shared.CreateContextOptions();

            var siteId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var forumId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var category = new Category(categoryId, siteId, "Category", 1, Guid.NewGuid());
            var forum = new Forum(forumId, category.Id, "Forum", "my-forum", "My Forum", 1);
            var topic = Post.CreateTopic(forumId, memberId, "Title", "slug", "Content", StatusType.Published);
            var member = new Member(memberId, Guid.NewGuid().ToString(), "Email", "Display Name");

            category.IncreaseTopicsCount();
            forum.IncreaseTopicsCount();
            member.IncreaseTopicsCount();

            using (var dbContext = new AtlifyDbContext(options))
            {
                dbContext.Categories.Add(category);
                dbContext.Forums.Add(forum);
                dbContext.Posts.Add(topic);
                dbContext.Members.Add(member);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlifyDbContext(options))
            {
                var command = Fixture.Build<DeleteTopic>()
                        .With(x => x.Id, topic.Id)
                        .With(x => x.ForumId, forumId)
                        .With(x => x.SiteId, siteId)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();
                var createValidator = new Mock<IValidator<CreateTopic>>();
                var updateValidator = new Mock<IValidator<UpdateTopic>>();

                var sut = new TopicService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.DeleteAsync(command);

                var topicDeleted = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == topic.Id);
                var topicEvent = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == topic.Id);

                var updatedCategory = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
                var updatedForum = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum.Id);
                var updatedMember = await dbContext.Members.FirstOrDefaultAsync(x => x.Id == member.Id);

                Assert.AreEqual(StatusType.Deleted, topicDeleted.Status);
                Assert.NotNull(topicEvent);
                Assert.AreEqual(0, updatedCategory.TopicsCount);
                Assert.AreEqual(0, updatedForum.TopicsCount);
                Assert.AreEqual(0, updatedMember.TopicsCount);
            }
        }
    }
}