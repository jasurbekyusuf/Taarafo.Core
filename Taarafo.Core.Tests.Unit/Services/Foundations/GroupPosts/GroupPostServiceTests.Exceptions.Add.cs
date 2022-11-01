﻿// ---------------------------------------------------------------
// Copyright (c) Coalition of the Good-Hearted Engineers
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Moq;
using Taarafo.Core.Models.GroupPosts.Exceptions;
using Taarafo.Core.Models.GroupPosts;
using Xunit;
using EFxceptions.Models.Exceptions;
using FluentAssertions;

namespace Taarafo.Core.Tests.Unit.Services.Foundations.GroupPosts
{
    public partial class GroupPostServiceTests
    {
        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddIfSqlErrorOccursAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTime = GetRandomDateTimeOffset();
            GroupPost randomGroupPost = CreateRandomGroupPost(randomDateTime);
            SqlException sqlException = GetSqlException();

            var failedGroupPostStorageException =
                new FailedGroupPostStorageException(sqlException);

            var expectedGroupPostDependencyException =
                new GroupPostDependencyException(failedGroupPostStorageException);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertGroupPostAsync(It.IsAny<GroupPost>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<GroupPost> addGroupPostTask =
                this.groupPostService.AddGroupPostAsync(randomGroupPost);

            // then
            await Assert.ThrowsAsync<GroupPostDependencyException>(() =>
                addGroupPostTask.AsTask());

            this.storageBrokerMock.Verify(broker =>
                broker.InsertGroupPostAsync(It.IsAny<GroupPost>()),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCritical(It.Is(SameExceptionAs(
                    expectedGroupPostDependencyException))),
                        Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfGroupPostAlreadyExistsAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTime = GetRandomDateTimeOffset();
            GroupPost randomGroupPost = CreateRandomGroupPost(randomDateTime);
            string randomMessage = GetRandomMessage();

            var duplicateKeyException =
                new DuplicateKeyException(randomMessage);

            var alreadyExistsGroupPostException =
                new AlreadyExistsGroupPostException(duplicateKeyException);

            var expectedGroupPostDependencyValidationException =
                new GroupPostDependencyValidationException(alreadyExistsGroupPostException);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertGroupPostAsync(It.IsAny<GroupPost>()))
                    .ThrowsAsync(duplicateKeyException);

            // when
            ValueTask<GroupPost> addGroupPostTask =
                this.groupPostService.AddGroupPostAsync(randomGroupPost);

            GroupPostDependencyValidationException actualGroupPostDependencyValidationException =
                await Assert.ThrowsAsync<GroupPostDependencyValidationException>(
                    addGroupPostTask.AsTask);

            // then
            actualGroupPostDependencyValidationException.Should().BeEquivalentTo(
                expectedGroupPostDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(SameExceptionAs(
                    expectedGroupPostDependencyValidationException))),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertGroupPostAsync(randomGroupPost),
                    Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
