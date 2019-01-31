﻿using FluentAssertions;
using nCubed.EFCore.Test.Entities;
using nCubed.EFCore.Test.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using nCubed.EFCore.Extensions;
using System.Linq;
using nCubed.EFCore.Test.Fakes;
using nCubed.EFCore.Test.Repositories.Fakes;

namespace nCubed.EFCore.Test.DataAccess
{
    public class Repository : Context, IDisposable
    {
        private readonly ProjectRepository projectRepository;
        private readonly CustomerRepository customerRepository;

        public Repository() : base()
        {
            projectRepository = new ProjectRepository(new ProjectsContext(dbContextOptions));
            customerRepository = new CustomerRepository(new ProjectsContext(dbContextOptions));
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestUnitOfWorkNull()
        {
            CustomerRepository customerRepository = null;

            Action act = () => customerRepository = new CustomerRepository(null);

            act.Should().Throw<ArgumentNullException>("DbSet is null");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestAddItemWithRelationships()
        {
            ContactInformation contactInformation = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            Customer customer = new Customer() { Name = "Customer", ContactInformation = contactInformation };
            Resource r = new Resource() { ContactInformation = contactInformation, Name = "Demo" };
            ProjectDetail projectDetail = new ProjectDetail() { Budget = 1000, Critical = true };
            Project p = new Project() { Customer = customer, Name = "Project", Description = "Description", End = DateTime.Now.AddDays(1), ProjectDetail = projectDetail, Start = DateTime.Now };


            projectRepository.Add(p);
            projectRepository.UnitOfWork.Commit();


            p.ProjectId.Should().NotBe(0, "User id is zero.");
            p.ProjectDetail.Project.Should().Be(p);
            p.Customer.CustomerId.Should().NotBe(0, "Customer id is zero.");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestFindWithoutIds()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customer = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer);
            customerRepository.UnitOfWork.Commit();

            Exception ex = Assert.Throws<ArgumentException>(() => customerRepository.Find());

            ex.Message.Should().Be("ids should not be empty");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestAddEntity()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customer = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer);
            customerRepository.UnitOfWork.Commit();

            var customerRepo = customerRepository.Find(1L);

            customerRepo.Should().NotBeNull();
            customerRepo.Name.Should().Be("Customer1");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestAddEntities()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customer = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer);
            customerRepository.UnitOfWork.Commit();

            var customerRepo1 = customerRepository.Find(1L);
            var customerRepo2 = customerRepository.Find(1L);

            customerRepo1.Should().NotBeNull();
            customerRepo2.Name.Should().Be("Customer1");
            customerRepo1.Should().NotBeNull();
            customerRepo2.Name.Should().Be("Customer1");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestDeleteEntity()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer1);
            customerRepository.UnitOfWork.Commit();

            var customerRepo1 = customerRepository.Find(1L);
            customerRepository.Delete(customerRepo1);
            customerRepository.UnitOfWork.Commit();
            var all = customerRepository.Set();

            all.Should().BeEmpty();
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestDeleteEntities()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };
            ContactInformation contactInformation2 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customer2 = new Customer() { Name = "Customer2", ContactInformation = contactInformation2 };

            customerRepository.Add(customer1, customer2);
            customerRepository.UnitOfWork.Commit();

            var customerRepo1 = customerRepository.Find(1L);
            var customerRepo2 = customerRepository.Find(2L);
            customerRepository.Delete(customerRepo1, customerRepo2);
            customerRepository.UnitOfWork.Commit();
            var all = customerRepository.Set();

            all.Should().BeEmpty();
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestGetAll()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            ContactInformation contactInformation2 = new ContactInformation() { Phone = "phone", Email = "demo@demo.es" };
            var customers = new List<Customer>{
                new Customer() { Name = "Customer1", ContactInformation = contactInformation1 },
                new Customer() { Name = "Customer2", ContactInformation = contactInformation2 },};

            customerRepository.Add(customers);
            customerRepository.UnitOfWork.Commit();

            var customersRepo = customerRepository.Set();

            customersRepo.Should().NotBeEmpty()
                .And.HaveCount(2)
                .And.Equal(customers, (c1, c2) => c1.Name == c2.Name)
                .And.NotContain((c) => c.CustomerId == 0);
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestApplyNewValues()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            ContactInformation contactInformation2 = new ContactInformation() { Phone = "phone2", Email = "demo2@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };
            var customer2 = new Customer() { Name = "Customer2", ContactInformation = contactInformation2 };


            customerRepository.Merge(customer1, customer2);

            customer1.Name.Should().Be(customer2.Name, "Different name.");
            customer1.ContactInformation.Phone.Should().NotBe(customer2.ContactInformation.Phone, "Same name.");
            customer1.ContactInformation.Email.Should().NotBe(customer2.ContactInformation.Email, "Same email.");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestReset()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer1);
            customerRepository.UnitOfWork.Commit();
            customer1.Name = "Name changed";
            customer1.ContactInformation.Email = "email 2";
            customer1.ContactInformation.Phone = "phone 2";

            CustomerRepository customerRepositoryAnotherThread = new CustomerRepository(new ProjectsContext(dbContextOptions));
            var customerChangedInAnotherThread = customerRepositoryAnotherThread.Set().Where(c => c.Name == "Customer1").First();
            customerChangedInAnotherThread.Name = "Name thread";
            customerChangedInAnotherThread.ContactInformation.Email = "email thread";
            customerChangedInAnotherThread.ContactInformation.Phone = "phone thread";
            customerRepositoryAnotherThread.UnitOfWork.Commit();

            customerRepository.Reset(customer1);
            customer1.Name.Should().Be("Customer1", "Different name.");
            customer1.ContactInformation.Phone.Should().NotBe("phone1", "Same telephone number");
            customer1.ContactInformation.Email.Should().NotBe("dem1@demo.es", "Same telephone number");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestRefresh()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer1);
            customerRepository.UnitOfWork.Commit();
            customer1.Name = "Name changed";
            customer1.ContactInformation.Email = "wrong email";
            customer1.ContactInformation.Phone = "wrong phone";
            customerRepository.Refresh(customer1);

            customer1.Name.Should().Be("Customer1", "Different name.");
            customer1.ContactInformation.Phone.Should().NotBe("phone1", "Same telephone number");
            customer1.ContactInformation.Email.Should().NotBe("dem1@demo.es", "Same telephone number");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestSource()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer1);
            customerRepository.UnitOfWork.Commit();

            CustomerRepository customerRepositoryAnotherThread = new CustomerRepository(new ProjectsContext(dbContextOptions));
            var customerChangedInAnotherThread = customerRepositoryAnotherThread.Set().Where(c => c.Name == "Customer1").First();
            customerChangedInAnotherThread.Name = "Name thread";
            customerChangedInAnotherThread.ContactInformation.Email = "email thread";
            customerChangedInAnotherThread.ContactInformation.Phone = "phone thread";
            customerRepositoryAnotherThread.UnitOfWork.Commit();

            customer1.Name = "22222";

            var entityFromDatabase = customerRepository.Source(customer1);

            entityFromDatabase.Name.Should().Be(customerChangedInAnotherThread.Name, "Different name");
        }

        [Fact]
        [Trait("Command", "Repository")]
        public void TestDetach()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            var customer1 = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer1);
            customerRepository.Detach(customer1);

            customerRepository.UnitOfWork.Local<Customer>().Should().BeEmpty();
        }

        [Fact]
        [Trait("Command", "Repository")]

        public void TestFind()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            var customer = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer);
            customerRepository.UnitOfWork.Commit();
            var customerRepo = customerRepository.Find(1L);

            customerRepo.Should().NotBeNull();
            customerRepo.Name.Should().Be("Customer1");
        }

        [Fact]
        [Trait("Command", "Repository")]

        public void TestExists()
        {
            ContactInformation contactInformation1 = new ContactInformation() { Phone = "phone1", Email = "demo1@demo.es" };
            var customer = new Customer() { Name = "Customer1", ContactInformation = contactInformation1 };

            customerRepository.Add(customer);
            customerRepository.UnitOfWork.Commit();
            var exists = customerRepository.Exists(1L);

            exists.Should().Be(true);
        }

        public void Dispose()
        {
            customerRepository.Dispose();
            projectRepository.Dispose();
        }
    }
}
