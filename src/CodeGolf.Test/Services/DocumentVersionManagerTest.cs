﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CodeGolf.Models;
using CodeGolf.Services;
using Microsoft.Azure.Documents.Client;
using NUnit.Framework;

namespace CodeGolf.Test.Services
{
    [TestFixture]
    public class DocumentVersionManagerTest
    {
        private DocumentDbService _service;
        private DocumentVersionManager _manager;

        [SetUp]
        public async Task SetUp()
        {
            var documentDbEmulator = new DocumentDbEmulator();
            documentDbEmulator.Start();

            _service = new DocumentDbService(Constants.DocumentDbConfig);

            await _service.Client.CreateDatabaseIfNotExists("CodeGolfDB");
            await _service.Client.CreateDocumentCollectionIfNotExists("CodeGolfDB", "CodeGolfCollection");
            await _service.Repository.Initialize();

            _manager = new DocumentVersionManager(_service);
        }

        [TearDown]
        public async Task TearDown()
        {
            var dbUri = UriFactory.CreateDatabaseUri("CodeGolfDB");
            await _service.Client.DeleteDatabaseAsync(dbUri);
        }

        [Test]
        public async Task ValidateVersion1_0()
        {
            var v = new Version1_0();
            Assert.AreEqual(new Version("1.0"), v.Version);

            await v.Step(_service);

            Assert.IsNotNull(FindLanguage("csharp"));
            Assert.IsNotNull(FindLanguage("powershell"));
            Assert.IsNotNull(FindLanguage("bat"));
            Assert.IsNotNull(FindLanguage("coffee"));
            Assert.IsNotNull(FindLanguage("cpp"));
            Assert.IsNotNull(FindLanguage("fsharp"));
            Assert.IsNotNull(FindLanguage("go"));
            Assert.IsNotNull(FindLanguage("jade"));
            Assert.IsNotNull(FindLanguage("java"));
            Assert.IsNotNull(FindLanguage("objective-c"));
            Assert.IsNotNull(FindLanguage("python"));
            Assert.IsNotNull(FindLanguage("r"));
            Assert.IsNotNull(FindLanguage("ruby"));
            Assert.IsNotNull(FindLanguage("sql"));
            Assert.IsNotNull(FindLanguage("swift"));
            Assert.IsNotNull(FindLanguage("vb"));
        }

        [Test]
        public async Task ValidateVersion1_1()
        {
            var v10 = new Version1_0();
            await v10.Step(_service);

            var v11 = new Version1_1();
            Assert.AreEqual(new Version("1.1"), v11.Version);
            await v11.Step(_service);

            Assert.IsFalse(FindLanguage("csharp").SupportsValidation);
            Assert.IsTrue(FindLanguage("powershell").SupportsValidation);
            Assert.IsFalse(FindLanguage("bat").SupportsValidation);
            Assert.IsFalse(FindLanguage("coffee").SupportsValidation);
            Assert.IsFalse(FindLanguage("cpp").SupportsValidation);
            Assert.IsFalse(FindLanguage("fsharp").SupportsValidation);
            Assert.IsFalse(FindLanguage("go").SupportsValidation);
            Assert.IsFalse(FindLanguage("jade").SupportsValidation);
            Assert.IsFalse(FindLanguage("java").SupportsValidation);
            Assert.IsFalse(FindLanguage("objective-c").SupportsValidation);
            Assert.IsFalse(FindLanguage("python").SupportsValidation);
            Assert.IsFalse(FindLanguage("r").SupportsValidation);
            Assert.IsFalse(FindLanguage("ruby").SupportsValidation);
            Assert.IsFalse(FindLanguage("sql").SupportsValidation);
            Assert.IsFalse(FindLanguage("swift").SupportsValidation);
            Assert.IsFalse(FindLanguage("vb").SupportsValidation);
        }

        [Test]
        public async Task ValidateVersion1_2()
        {
            await _manager.Upgrade(new Version(1, 1));

            var v12 = new Version1_2();
            Assert.AreEqual(new Version(1,2), v12.Version);

            var problem = new Problem();
            problem.Input = "Input";
            problem.Output = "Output";
            await _service.Client.CreateDocumentAsync(_service.DatabaseUri, problem);
            await v12.Step(_service);

            var updatedProblem = _service.Client.CreateDocumentQuery<Problem>(_service.DatabaseUri)
                .Where(m => m.Type == DocumentType.Problem)
                .ToList()
                .First();

            Assert.AreEqual("Input", updatedProblem.TestCases.First().Input);
            Assert.AreEqual("Output", updatedProblem.TestCases.First().Output);
        }

        [Test]
        public async Task ValidateVersion1_3()
        {
            await _manager.Upgrade(new Version(1, 2));

            //Duplicate all the languages
            foreach (var language in _service.Client.CreateDocumentQuery<Language>(_service.DatabaseUri))
            {
                var newLanguage = new Language
                {
                    Id = Guid.NewGuid(),
                    Name = language.Name,
                    DisplayName = language.DisplayName
                };

                await _service.Client.CreateDocumentAsync(_service.DatabaseUri, newLanguage);
            }

            var v13 = new Version1_3();
            Assert.AreEqual(new Version(1, 3), v13.Version);
            await v13.Step(_service);

            //Make sure there are no duplicate languages
            foreach (
                var group in
                _service.Client.CreateDocumentQuery<Language>(_service.DatabaseUri)
                    .Where(m => m.Type == DocumentType.Language)
                    .ToList()
                    .GroupBy(m => m.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() }))
            {
                Assert.AreEqual(1, group.Count);
            }
        }

        [Test]
        public async Task ValidateVersion1_4()
        {
            await _manager.Upgrade(new Version(1, 3));

            var v14 = new Version1_4();
            Assert.AreEqual(new Version(1,4), v14.Version);

            await v14.Step(_service);

            Assert.IsTrue(FindLanguage("csharp").SupportsValidation);
            Assert.IsTrue(FindLanguage("powershell").SupportsValidation);
            Assert.IsFalse(FindLanguage("bat").SupportsValidation);
            Assert.IsFalse(FindLanguage("coffee").SupportsValidation);
            Assert.IsFalse(FindLanguage("cpp").SupportsValidation);
            Assert.IsFalse(FindLanguage("fsharp").SupportsValidation);
            Assert.IsFalse(FindLanguage("go").SupportsValidation);
            Assert.IsFalse(FindLanguage("jade").SupportsValidation);
            Assert.IsFalse(FindLanguage("java").SupportsValidation);
            Assert.IsFalse(FindLanguage("objective-c").SupportsValidation);
            Assert.IsFalse(FindLanguage("python").SupportsValidation);
            Assert.IsFalse(FindLanguage("r").SupportsValidation);
            Assert.IsFalse(FindLanguage("ruby").SupportsValidation);
            Assert.IsFalse(FindLanguage("sql").SupportsValidation);
            Assert.IsFalse(FindLanguage("swift").SupportsValidation);
            Assert.IsFalse(FindLanguage("vb").SupportsValidation);
        }

        [Test]
        public async Task ValidateVersion1_5_ShouldCreateUserInNewCollection()
        {
            await _manager.Upgrade(new Version(1, 4));

            var v15 = new Version1_5();
            Assert.AreEqual(new Version(1,5), v15.Version);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Identity = "Adam"
            };

            await _service.Client.CreateDocumentAsync(_service.DatabaseUri, user);
            
            await v15.Step(_service);

            var newUser = await _service.Repository.Users.Get(user.Id);

            Assert.AreEqual(user.Identity, newUser.Identity);
        }

        [Test]
        public async Task ValidateVersion1_5_ShouldDeleteUserFromOldCollection()
        {
            await _manager.Upgrade(new Version(1, 4));

            var v15 = new Version1_5();
            Assert.AreEqual(new Version(1, 5), v15.Version);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Identity = "Adam"
            };

            await _service.Client.CreateDocumentAsync(_service.DatabaseUri, user);

            var myUser =
                await _service.Client.ReadDocumentAsync(UriFactory.CreateDocumentUri("CodeGolfDB", "CodeGolfCollection",
                    user.Id.ToString()));
                
            Assert.IsNotNull(myUser);

            await v15.Step(_service);

            try
            {
                await _service.Client.ReadDocumentAsync(UriFactory.CreateDocumentUri("CodeGolfDB", "CodeGolfCollection",
                    user.Id.ToString()));

                Assert.Fail("Should not find resource.");
            } catch { }
        }

        [Test]
        public async Task ValidateVersion1_6_ShouldCreateProblemInNewCollection()
        {
            await _manager.Upgrade(new Version(1, 5));

            var v16 = new Version1_6();
            Assert.AreEqual(new Version(1, 6), v16.Version);

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Description = "My Problem"
            };

            await _service.Client.CreateDocumentAsync(_service.DatabaseUri, problem);

            await v16.Step(_service);

            var newUser = await _service.Repository.Problem.Get(problem.Id);

            Assert.AreEqual(problem.Description, newUser.Description);
        }

        [Test]
        public async Task ValidateVersion1_6_ShouldSetLanguageOnNewProblem()
        {
            await _manager.Upgrade(new Version(1, 5));

            var v16 = new Version1_6();
            Assert.AreEqual(new Version(1, 6), v16.Version);

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Description = "My Problem",
                Language = _service.Client.CreateDocumentQuery<Language>(UriFactory.CreateDocumentCollectionUri(Constants.Database, Constants.Collection)).Where(m => m.Name == "csharp").ToList().First().Id
            };

            await _service.Client.CreateDocumentAsync(_service.DatabaseUri, problem);

            await v16.Step(_service);

            var newProblem = await _service.Repository.Problem.Get(problem.Id);

            Assert.AreEqual("csharp", newProblem.LanguageModel.Name);
        }

        [Test]
        public async Task ValidateVersion1_6_ShouldDeleteProblemFromOldCollection()
        {
            await _manager.Upgrade(new Version(1, 5));

            var v16 = new Version1_6();
            Assert.AreEqual(new Version(1, 6), v16.Version);

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Description = "My Problem"
            };

            await _service.Client.CreateDocumentAsync(_service.DatabaseUri, problem);

            var myProblem =
                await _service.Client.ReadDocumentAsync(UriFactory.CreateDocumentUri("CodeGolfDB", "CodeGolfCollection",
                    problem.Id.ToString()));

            Assert.IsNotNull(myProblem);

            await v16.Step(_service);

            try
            {
                await _service.Client.ReadDocumentAsync(UriFactory.CreateDocumentUri("CodeGolfDB", "CodeGolfCollection",
                    problem.Id.ToString()));

                Assert.Fail("Should not find resource.");
            }
            catch { }
        }

        [Test]
        public async Task ValidateVersion1_7_ShouldUpdateLanguageOnProblems()
        {
            await _manager.Upgrade(new Version(1, 6));

            var v17 = new Version1_7();
            Assert.AreEqual(new Version(1,7), v17.Version);

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Description = "My Problem",
                Language = FindLanguage("csharp").Id,
                LanguageModel = FindLanguage("csharp")
            };

            await _service.Repository.Problem.Create(problem);
            await v17.Step(_service);

            var updatedProblem = await _service.Repository.Problem.Get(problem.Id);

            Assert.IsNull(updatedProblem.LanguageModel);
        }

        [Test]
        public async Task ValidateVersion1_7_ShouldDeleteLanguagesFromDatabase()
        {
            await _manager.Upgrade(new Version(1, 6));

            var v17 = new Version1_7();
            Assert.AreEqual(new Version(1, 7), v17.Version);

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Description = "My Problem",
                Language = FindLanguage("csharp").Id,
                LanguageModel = FindLanguage("csharp")
            };

            await _service.Repository.Problem.Create(problem);
            await v17.Step(_service);

            var anyLanguages =
                _service.Client.CreateDocumentQuery<Language>(_service.DatabaseUri)
                    .Where(m => m.Type == DocumentType.Language).ToList().Any();

            Assert.IsFalse(anyLanguages);
        }

        private Language FindLanguage(string name)
        {
            return _service.Client.CreateDocumentQuery<Language>(_service.DatabaseUri).Where(m => m.Name == name).ToList().FirstOrDefault();
        }
    }
}
