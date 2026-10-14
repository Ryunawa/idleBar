using System;
using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed class Crier
{
    private const float ShowSeconds = 7;
    private const float MinGap = 120;
    private const float MaxGap = 180;
    private const float FirstGap = 20;

    private static readonly IReadOnlyList<string> News =
    [
        "Les slimes réclament le droit de grève.",
        "Le moulin tourne à l'envers depuis mardi.",
        "Un gobelin ouvre une boulangerie. Elle est bonne.",
        "La forge a forgé une deuxième forge.",
        "Pénurie de carottes : les ânes se syndiquent.",
        "Le mage jure que le portail est sans danger.",
        "Une poule dorée aperçue au-dessus des toits.",
        "Le héros refuse de payer son pain.",
        "Le brouillard recule. Les assureurs aussi.",
        "Les squelettes demandent des cours de danse.",
        "Un loup a mangé le panneau « Attention au loup ».",
        "Les champignons de la forêt sont vexés.",
        "Le désert manque de sable. Enquête en cours.",
        "Un yéti cherche un pull à sa taille.",
        "Le dragon prétend qu'il était là avant.",
        "Un ranger a raté une cible. Volontairement.",
        "Le marché vend du brouillard en bocal.",
        "Le meunier jure que sa farine tient une journée entière.",
    ];

    private float _next = FirstGap;
    private int _index;

    public string Text { get; private set; } = string.Empty;

    public float Showing { get; private set; }

    public void Update(float dt, Random random)
    {
        Showing = Math.Max(0, Showing - dt);
        _next -= dt;
        if (_next > 0)
        {
            return;
        }

        Text = News[_index % News.Count];
        _index += 1 + random.Next(2);
        Showing = ShowSeconds;
        _next = MinGap + (float)random.NextDouble() * (MaxGap - MinGap);
    }
}
