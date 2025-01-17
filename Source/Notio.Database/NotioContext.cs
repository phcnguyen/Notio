﻿using Microsoft.EntityFrameworkCore;
using Notio.Database.Model;

namespace Notio.Database;

public sealed class NotioContext(DbContextOptions<NotioContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<UserChat> UserChats { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelConfiguration.UserEntity(modelBuilder);
        ModelConfiguration.ChatEntity(modelBuilder);
        ModelConfiguration.MessageEntity(modelBuilder);
        ModelConfiguration.UserChatEntity(modelBuilder);
        ModelConfiguration.MessageAttachmentEntity(modelBuilder);
    }
}