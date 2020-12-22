﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Smartstore.Core.Checkout.GiftCards
{
    public class GiftCardMap : IEntityTypeConfiguration<GiftCard>
    {
        public void Configure(EntityTypeBuilder<GiftCard> builder)
        {
            builder.Property(gc => gc.Value).HasPrecision(18, 4);

            // TODO: (core) (ms) OrderItem is needed
            //builder.HasOne(gc => gc.OrderItem)
            //       .WithMany(orderItem => orderItem.AssociatedGiftCards)
            //       .HasForeignKey(gc => gc.OrderItemId);
        }
    }

    /// <summary>
    /// Represents a gift card
    /// </summary>
    public class GiftCard : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public GiftCard()
        {
        }

        public GiftCard(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the gift card type identifier
        /// </summary>
        public int GiftCardTypeId { get; set; }

        /// <summary>
        /// Gets or sets the gift card type
        /// </summary>
        [NotMapped]
        public GiftCardType GiftCardType
        {
            get => (GiftCardType)GiftCardTypeId;
            set => GiftCardTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets the associated order item identifier
        /// </summary>
        public int? OrderItemId { get; set; }

        /// <summary>
        /// Gets or sets the amount
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gift card is activated
        /// </summary>
        public bool IsActivated { get; set; }

        /// <summary>
        /// Gets or sets a gift card coupon code
        /// </summary>
        public string CouponCode { get; set; }

        /// <summary>
        /// Gets or sets a recipient name
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// Gets or sets a recipient email
        /// </summary>
        public string RecipientEmail { get; set; }

        /// <summary>
        /// Gets or sets a sender name
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets a sender email
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets a message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether recipient is notified
        /// </summary>
        public bool IsRecipientNotified { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private ICollection<GiftCardUsageHistory> _giftCardUsageHistory;
        /// <summary>
        /// Gets or sets the gift card usage history
        /// </summary> 
        public ICollection<GiftCardUsageHistory> GiftCardUsageHistory
        {
            get => _lazyLoader?.Load(this, ref _giftCardUsageHistory) ?? (_giftCardUsageHistory ??= new HashSet<GiftCardUsageHistory>());
            init => _giftCardUsageHistory = value;
        }

        // TODO: (core) (ms) OrderItem is needed
        //private OrderItem _orderItem;
        ///// <summary>
        ///// Gets or sets the associated order item
        ///// </summary>
        //public OrderItem OrderItem
        //{
        //    get => _lazyLoader?.Load(this, ref _orderItem) ?? _orderItem;
        //    set => _orderItem = value;
        //}

        #region Methods

        /// <summary>
        /// Gets gift cards remaining value
        /// </summary>
        /// <returns>Gift card remaining value</returns>
        public decimal GetRemainingValue()
        {
            var result = Value - GiftCardUsageHistory.Sum(x => x.UsedValue);
            return result < decimal.Zero
                ? decimal.Zero
                : result;
        }

        // TODO: (core) (ms) OrderItem is needed
        ///// <summary>
        ///// Checks whether the gift card is valid for store and has a positive balance
        ///// </summary>
        ///// <param name="storeId">Storeidentifier. 0 validates the gift card for all stores</param>
        ///// <returns>True - valid; False - invalid</returns>
        //public bool IsValid(int storeId = 0)
        //{
        //    if (!IsActivated)
        //        return false;

        //    var orderStoreId = OrderItem?.Order?.StoreId ?? null;
        //    if (storeId == 0 || orderStoreId is null || orderStoreId == storeId)
        //        return GetGiftCardRemainingValue() > decimal.Zero;

        //    return false;
        //}

        #endregion
    }
}
