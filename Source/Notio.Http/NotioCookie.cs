﻿using Notio.Http.Cookie;
using Notio.Http.Enums;
using Notio.Http.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Notio.Http;

/// <summary>
/// Represents an HTTP cookie. Closely matches Set-Cookie response header.
/// </summary>
public class NotioCookie
{
    private string _value;
    private DateTimeOffset? _expires;
    private int? _maxAge;
    private string _domain;
    private string _path;
    private bool _secure;
    private bool _httpOnly;
    private SameSite? _sameSite;

    private bool _locked;

    /// <summary>
    /// Creates a new NotioCookie.
    /// </summary>
    /// <param name="name">Name of the cookie.</param>
    /// <param name="value">Value of the cookie.</param>
    /// <param name="originUrl">URL of request that sent the original Set-Cookie header.</param>
    /// <param name="dateReceived">Date/time that original Set-Cookie header was received. Defaults to current date/time. Important for Max-Age to be enforced correctly.</param>
    public NotioCookie(string name, string value, string originUrl = null, DateTimeOffset? dateReceived = null)
    {
        Name = name;
        Value = value;
        OriginUrl = originUrl;
        DateReceived = dateReceived ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// The URL that originally sent the Set-Cookie response header. If adding to a CookieJar, this is required unless
    /// both Domain AND Path are specified.
    /// </summary>
    public Url OriginUrl { get; }

    /// <summary>
    /// Date and time the cookie was received. Defaults to date/time this NotioCookie was created.
    /// Important for Max-Age to be enforced correctly.
    /// </summary>
    public DateTimeOffset DateReceived { get; }

    /// <summary>
    /// The cookie name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The cookie value.
    /// </summary>
    public string Value
    {
        get => _value;
        set => Update(ref _value, value);
    }

    /// <summary>
    /// Corresponds to the Expires attribute of the Set-Cookie header.
    /// </summary>
    public DateTimeOffset? Expires
    {
        get => _expires;
        set => Update(ref _expires, value);
    }

    /// <summary>
    /// Corresponds to the Max-Age attribute of the Set-Cookie header.
    /// </summary>
    public int? MaxAge
    {
        get => _maxAge;
        set => Update(ref _maxAge, value);
    }

    /// <summary>
    /// Corresponds to the Domain attribute of the Set-Cookie header.
    /// </summary>
    public string Domain
    {
        get => _domain;
        set => Update(ref _domain, value);
    }

    /// <summary>
    /// Corresponds to the Path attribute of the Set-Cookie header.
    /// </summary>
    public string Path
    {
        get => _path;
        set => Update(ref _path, value);
    }

    /// <summary>
    /// Corresponds to the Secure attribute of the Set-Cookie header.
    /// </summary>
    public bool Secure
    {
        get => _secure;
        set => Update(ref _secure, value);
    }

    /// <summary>
    /// Corresponds to the HttpOnly attribute of the Set-Cookie header.
    /// </summary>
    public bool HttpOnly
    {
        get => _httpOnly;
        set => Update(ref _httpOnly, value);
    }

    /// <summary>
    /// Corresponds to the SameSite attribute of the Set-Cookie header.
    /// </summary>
    public SameSite? SameSite
    {
        get => _sameSite;
        set => Update(ref _sameSite, value);
    }

    /// <summary>
    /// Generates a key based on cookie Name, Domain, and Path (using OriginalUrl in the absence of Domain/Path).
    /// Used by CookieJar to determine whether to add a cookie or update an existing one.
    /// </summary>
    public string GetKey()
    {
        var domain = string.IsNullOrEmpty(Domain) ? "*" + OriginUrl.Host : Domain;
        var path = string.IsNullOrEmpty(Path) ? OriginUrl.Path : Path;
        if (path.Length == 0) path = "/";
        return $"{domain}{path}:{Name}";
    }

    /// <summary>
    /// Writes this cookie to a TextWriter. Useful for persisting to a file.
    /// </summary>
    public void WriteTo(TextWriter writer)
    {
        writer.WriteLine(DateReceived.ToString("s"));
        writer.WriteLine(OriginUrl);
        writer.WriteLine(CookieCutter.BuildResponseHeader(this));
    }

    /// <summary>
    /// Instantiates a NotioCookie that was previously persisted using WriteTo.
    /// </summary>
    public static NotioCookie LoadFrom(TextReader reader)
    {
        if (!DateTimeOffset.TryParse(reader?.ReadLine(), null, DateTimeStyles.AssumeUniversal, out var received))
            return null;

        var url = reader.ReadLine();
        if (string.IsNullOrEmpty(url)) return null;

        var headerVal = reader.ReadLine();
        if (string.IsNullOrEmpty(headerVal)) return null;

        return CookieCutter.ParseResponseHeader(headerVal, url, received);
    }

    /// <summary>
    /// Returns a string representing this NotioCookie.
    /// </summary>
    public override string ToString()
    {
        var writer = new StringWriter();
        WriteTo(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Instantiates a NotioCookie that was previously persisted using ToString.
    /// </summary>
    public static NotioCookie LoadFromString(string s) => LoadFrom(new StringReader(s));


    /// <summary>
    /// Makes this cookie immutable. Call when added to a jar.
    /// </summary>
    internal void Lock()
    {
        _locked = true;
    }

    private void Update<T>(ref T field, T newVal, [CallerMemberName] string propName = null)
    {
        // == throws with generics (strangely), and .Equals needs a null check. Jon Skeet to the rescue.
        // https://stackoverflow.com/a/390974/62600
        if (EqualityComparer<T>.Default.Equals(field, newVal))
            return;

        if (_locked)
            throw new Exception("After a cookie has been added to a CookieJar, it becomes immutable and cannot be changed.");

        field = newVal;
    }
}