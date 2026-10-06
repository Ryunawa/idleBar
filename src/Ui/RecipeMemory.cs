using Godot;

namespace IdleBar.Ui;

public sealed class RecipeMemory
{
    private const string Section = "workshop";
    private const string LastRecipeKey = "last_recipe";

    private readonly string _path;

    private RecipeMemory(string path, string? lastRecipeId)
    {
        _path = path;
        LastRecipeId = lastRecipeId;
    }

    public string? LastRecipeId { get; private set; }

    public static RecipeMemory Load(string path)
    {
        ConfigFile file = new();
        string lastRecipeId = file.Load(path) == Error.Ok ? file.GetValue(Section, LastRecipeKey, string.Empty).AsString() : string.Empty;
        return new RecipeMemory(path, lastRecipeId.Length == 0 ? null : lastRecipeId);
    }

    public void Remember(string recipeId)
    {
        if (recipeId == LastRecipeId)
        {
            return;
        }

        LastRecipeId = recipeId;
        ConfigFile file = new();
        file.SetValue(Section, LastRecipeKey, recipeId);
        Error result = file.Save(_path);
        if (result != Error.Ok)
        {
            GD.PushWarning($"Dernière recette non enregistrée dans {_path} : {result}");
        }
    }
}
