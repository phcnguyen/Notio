﻿using System;
using System.Collections.Generic;

namespace Notio.Database.Model;

/// <summary>
/// Đại diện cho cuộc trò chuyện trong cơ sở dữ liệu.
/// </summary>
public class Chat
{
    public long ChatId { get; set; }
    public string ChatName { get; set; }
    public bool IsGroupChat { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Navigation properties
    public ICollection<UserChat> UserChats { get; set; }

    public ICollection<Message> Messages { get; set; }
}