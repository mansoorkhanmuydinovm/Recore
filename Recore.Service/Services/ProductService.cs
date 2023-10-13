﻿using AutoMapper;
using Recore.Data.IRepositories;
using Recore.Service.Exceptions;
using Recore.Service.Extensions;
using Recore.Service.Interfaces;
using Recore.Domain.Configurations;
using Recore.Service.DTOs.Products;
using Recore.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Recore.Domain.Entities.Products;
using Recore.Service.DTOs.Attachments;
using Recore.Domain.Entities.Inventories;

namespace Recore.Service.Services;

public class ProductService : IProductService
{
    private readonly IMapper mapper;
    private readonly IAttachmentService attachmentService;
    private readonly IRepository<Product> productRepository;
    private readonly IRepository<OrderItem> orderItemRepository;
    private readonly IRepository<Inventory> inventoryRepository;
    private readonly IRepository<ProductCategory> productCategoryRepository;
    public ProductService(
        IMapper mapper,
        IAttachmentService attachmentService,
        IRepository<Product> productRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Inventory> inventoryRepository)
    {
        this.mapper = mapper;
        this.productRepository = productRepository;
        this.attachmentService = attachmentService;
        this.productCategoryRepository = productCategoryRepository;
        this.orderItemRepository = orderItemRepository;
        this.inventoryRepository = inventoryRepository;
    }

    public async ValueTask<ProductResultDto> AddAsync(ProductCreationDto dto)
    {
        var product = await this.productRepository.SelectAsync(p => p.Name.Equals(dto.Name), includes: new[] { "Attachment" });
        if (product is not null)
            throw new AlreadyExistException($"This {product.Name.ToLower()} is alread exists");

        var category = await this.productCategoryRepository.SelectAsync(p => p.Id.Equals(dto.CategoryId))
            ?? throw new NotFoundException("This category is not found");

        var mappedProduct = this.mapper.Map<Product>(dto);
        mappedProduct.CategoryId = category.Id;
        await this.productRepository.CreateAsync(mappedProduct);
        await this.productRepository.SaveAsync();
        mappedProduct.Category = category;

        return this.mapper.Map<ProductResultDto>(mappedProduct);
    }


    public async ValueTask<ProductResultDto> ModifyAsync(ProductUpdateDto dto)
    {
        var product = await this.productRepository.SelectAsync(p => p.Id.Equals(dto.Id), includes: new[] { "Attachment" })
            ?? throw new NotFoundException("This product is not found");

        var category = await this.productCategoryRepository.SelectAsync(p => p.Id.Equals(dto.CategoryId))
            ?? throw new NotFoundException("This category is not found");
        
        product.CategoryId = category.Id;
        product.Category = category;
        this.mapper.Map(dto, product);
        this.productRepository.Update(product);
        await this.productRepository.SaveAsync();

        return this.mapper.Map<ProductResultDto>(product);
    }

    public async ValueTask<bool> RemoveAsync(long id)
    {
        var product = await this.productRepository.SelectAsync(p => p.Id.Equals(id))
            ?? throw new NotFoundException("This product is not found");

        this.productRepository.Delete(product);
        await this.productRepository.SaveAsync();
        return true;
    }

    public async ValueTask<IEnumerable<ProductResultDto>> RetrieveAllAsync(PaginationParams @params, Filter filter)
    {
        var products = await this.productRepository.SelectAll(includes: new[] { "Category", "Attachment" })
            .OrderBy(filter)
            .ToPaginate(@params)
            .ToListAsync();

        var result = this.mapper.Map<IEnumerable<ProductResultDto>>(products);

        foreach (var product in result)
        {
            var inventory = await this.inventoryRepository
                .SelectAsync(inventory => inventory.ProductId.Equals(product.Id));

            if(inventory is not null)
            {
                product.Quantity = inventory.Quantity;
                if (inventory.Quantity > 0)
                    product.IsAvailable = true;
            }
        }

        return result;
    }

    public async ValueTask<IEnumerable<ProductResultDto>> RetrieveAllAsync()
    {
        var products = await this.productRepository.SelectAll(includes: new[] { "Category", "Attachment" })
            .ToListAsync();

        var result = this.mapper.Map<IEnumerable<ProductResultDto>>(products);

        foreach (var product in result)
        {
            var inventory = await this.inventoryRepository
                .SelectAsync(inventory => inventory.ProductId.Equals(product.Id));

            if (inventory is not null)
            {
                product.Quantity = inventory.Quantity;
                if (inventory.Quantity > 0)
                    product.IsAvailable = true;
            }
        }

        return result;
    }

    public async ValueTask<IEnumerable<ProductResultDto>> RetrieveAllAsync(long categoryId)
    {
        var products = await this.productRepository.SelectAll(expression: p => p.CategoryId == categoryId,
            includes: new[] { "Category", "Attachment" }).ToListAsync();

        var result = this.mapper.Map<IEnumerable<ProductResultDto>>(products);

        foreach (var product in result)
        {
            var inventory = await this.inventoryRepository
                .SelectAsync(inventory => inventory.ProductId.Equals(product.Id));

            if (inventory is not null)
            {
                product.Quantity = inventory.Quantity;
                if (inventory.Quantity > 0)
                    product.IsAvailable = true;
            }
        }

        return result;
    }

    public async ValueTask<ProductResultDto> RetrieveByIdAsync(long id)
    {
        var product = await this.productRepository.SelectAsync(p => p.Id.Equals(id), 
            includes: new[] { "Category", "Attachment" })
            ?? throw new NotFoundException("This product is not found");

        var inventory = await this.inventoryRepository
               .SelectAsync(inventory => inventory.ProductId.Equals(product.Id));
        
        var result = this.mapper.Map<ProductResultDto>(product);

        if (inventory is not null)
        {
            result.Quantity = inventory.Quantity;
            if (inventory.Quantity > 0)
                product.IsAvailable = true;
        }

        return result;
    }

    public async ValueTask<ProductResultDto> ImageUploadAsync(long productId, AttachmentCreationDto dto)
    {
        var product = await this.productRepository.SelectAsync(p => p.Id.Equals(productId), includes: new[] { "Category" })
            ?? throw new NotFoundException("This product is not found");

        var createdAttachment = await this.attachmentService.UploadAsync(dto);
        product.AttachmentId = createdAttachment.Id;
        product.Attachment = createdAttachment;

        this.productRepository.Update(product);
        await this.productRepository.SaveAsync();

        return this.mapper.Map<ProductResultDto>(product);
    }

    public async ValueTask<ProductResultDto> ModifyImageAsync(long productId, AttachmentCreationDto dto)
    {
        var product = await this.productRepository.SelectAsync(p => p.Id.Equals(productId), 
            includes: new[] { "Category", "Attachment" })
            ?? throw new NotFoundException("This product is not found");

        await this.attachmentService.RemoveAsync(product.Attachment);
        var createdAttachment = await this.attachmentService.UploadAsync(dto);

        product.AttachmentId = createdAttachment.Id;
        product.Attachment = createdAttachment;
        this.productRepository.Update(product);
        await this.productRepository.SaveAsync();

        return this.mapper.Map<ProductResultDto>(product);
    }
}
