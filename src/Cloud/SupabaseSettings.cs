using Godot;

namespace IdleBar.Cloud;

public sealed record SupabaseSettings(string Url, string PublishableKey)
{
    private const string UrlSetting = "idlebar/cloud/supabase_url";
    private const string PublishableKeySetting = "idlebar/cloud/supabase_publishable_key";

    public static SupabaseSettings? FromProjectSettings()
    {
        string url = ProjectSettings.GetSetting(UrlSetting, string.Empty).AsString();
        string publishableKey = ProjectSettings.GetSetting(PublishableKeySetting, string.Empty).AsString();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(publishableKey))
        {
            return null;
        }

        return new SupabaseSettings(url.TrimEnd('/'), publishableKey);
    }
}
