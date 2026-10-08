alter table private.settings add column if not exists souvenir_reward integer not null default 250 check (souvenir_reward >= 0);

create table if not exists public.regulars (
  id text primary key,
  name text not null,
  title text not null,
  drink text not null references public.drinks (id),
  condition text not null,
  hint text not null,
  souvenir text not null,
  sort integer not null
);

insert into public.regulars (id, name, title, drink, condition, hint, souvenir, sort) values
  ('gaspard', 'Gaspard', 'chevalier à la retraite', 'beer', 'any', 'Vient à toute heure.', 'L''épée émoussée de Gaspard', 1),
  ('melisande', 'Mélisande', 'herboriste', 'tea', 'day', 'Vient le jour.', 'Un bouquet de simples', 2),
  ('brindille', 'Brindille', 'apprentie sorcière', 'tea', 'night', 'Vient la nuit.', 'Un crapaud en porcelaine', 3),
  ('odette', 'Odette', 'factrice toujours pressée', 'beer', 'day', 'Vient le jour, en coup de vent.', 'La lettre que personne ne réclamait', 4),
  ('fantome', 'Le Fantôme', 'hôte de la maison', 'tea', 'night', 'Ne vient que la nuit.', 'Un chandelier froid', 5),
  ('bartholome', 'Bartholomé', 'barde', 'cider', 'decor:fireplaces', 'Vient s''il y a une cheminée.', 'Le luth de Bartholomé', 6),
  ('ysolde', 'Ysolde', 'capitaine de navire', 'beer', 'rain', 'Vient les jours de pluie.', 'Une ancre de marine', 7),
  ('pip', 'Pip', 'gobelin affamé', 'soup', 'menu:soup', 'Vient s''il y a de la soupe.', 'Un champignon porte-bonheur', 8),
  ('anselme', 'Frère Anselme', 'moine de l''abbaye', 'cider', 'menu:cider', 'Vient s''il y a du cidre.', 'Une pomme bénie', 9),
  ('lune', 'Madame Lune', 'astrologue', 'tea', 'clear-night', 'Vient les nuits sans pluie.', 'Une étoile de cuivre', 10),
  ('fennec', 'Fennec', 'voleur repenti', 'beer', 'tier:1', 'Vient quand la taverne devient un Estaminet.', 'Le dernier crochet de Fennec', 11),
  ('hugues', 'Hugues', 'marchand de cartes', 'pie', 'menu:pie', 'Vient s''il y a de la tourte.', 'La carte du royaume', 12)
on conflict (id) do update set
  name = excluded.name, title = excluded.title, drink = excluded.drink, condition = excluded.condition,
  hint = excluded.hint, souvenir = excluded.souvenir, sort = excluded.sort;

create table if not exists public.regular_chapters (
  regular_id text not null references public.regulars (id),
  chapter integer not null check (chapter between 1 and 5),
  friendship integer not null check (friendship > 0),
  line text not null,
  primary key (regular_id, chapter)
);

insert into public.regular_chapters (regular_id, chapter, friendship, line)
select regular_id, chapter, (array[3, 8, 15, 25, 40])[chapter], line
from (values
  ('gaspard', 1, 'Trente ans d''armure, et c''est mon dos qui a rendu les armes en premier.'),
  ('gaspard', 2, 'J''ai combattu un dragon, autrefois. Enfin… un très gros lézard. Mais il crachait.'),
  ('gaspard', 3, 'Ma fille veut que j''écrive mes mémoires. Je ne sais pas écrire, et je me souviens mal.'),
  ('gaspard', 4, 'Ici, personne ne m''appelle « messire ». Ça me repose.'),
  ('gaspard', 5, 'Garde mon épée. Elle coupe moins bien que ton pain, mais elle a du cœur.'),
  ('melisande', 1, 'La camomille pour dormir, la menthe pour réfléchir… et ton thé pour le reste.'),
  ('melisande', 2, 'J''ai trouvé de l''ail des ours près du moulin. Ne le dis à personne.'),
  ('melisande', 3, 'On me prend pour une sorcière. Je suis juste quelqu''un qui lit les étiquettes.'),
  ('melisande', 4, 'Ton thé infuse comme il faut. C''est plus rare qu''on croit.'),
  ('melisande', 5, 'Accroche ce bouquet au-dessus du comptoir : il chasse les mauvaises humeurs.'),
  ('brindille', 1, 'Tu n''aurais pas vu un crapaud ? Il répond au nom de Monsieur Bouton.'),
  ('brindille', 2, 'Mon maître dit qu''un crapaud perdu revient toujours. Il disait pareil de son chapeau.'),
  ('brindille', 3, 'J''ai lancé un sort de recherche. J''ai retrouvé trois cuillères et une chaussette.'),
  ('brindille', 4, 'On a vu un crapaud chanter près de la fontaine. C''est lui : il chante faux.'),
  ('brindille', 5, 'Je l''ai retrouvé ! Il dormait dans ma botte. Prends ce crapaud en porcelaine, pour fêter ça.'),
  ('odette', 1, 'Pas le temps, pas le temps… Bon, une gorgée.'),
  ('odette', 2, 'J''ai une lettre que personne ne réclame depuis dix ans. Je la garde toujours sur moi.'),
  ('odette', 3, 'Je connais toutes les portes de la ville. La tienne est la seule où l''on m''offre à boire.'),
  ('odette', 4, 'Aujourd''hui, j''ai pris le temps de m''asseoir. Ne le dis pas au receveur.'),
  ('odette', 5, 'Cette lettre, je crois qu''elle t''était destinée, finalement. Garde-la.'),
  ('fantome', 1, 'Hou… Pardon. C''est l''habitude.'),
  ('fantome', 2, 'Je hantais cette maison bien avant la taverne. Le bruit des chopes me plaît mieux que les grincements.'),
  ('fantome', 3, 'Le thé me traverse, mais son odeur me réchauffe.'),
  ('fantome', 4, 'J''ai oublié mon nom. Ici, on m''appelle « le Fantôme ». C''est déjà ça.'),
  ('fantome', 5, 'Je te laisse mon chandelier. Allume-le les soirs de brume, et je passerai.'),
  ('bartholome', 1, 'Une chanson pour une pinte ? Non ? Une pinte pour une chanson, alors.'),
  ('bartholome', 2, 'J''écris une ballade sur ta taverne. Rien ne rime avec « étain ».'),
  ('bartholome', 3, 'Ce feu crépite en rythme. Je crois qu''il m''accompagne.'),
  ('bartholome', 4, 'Le public d''ici est le meilleur : il ne sait pas que je chante faux.'),
  ('bartholome', 5, 'Mon luth a trop voyagé. Garde-le près de la cheminée, il y sera bien.'),
  ('ysolde', 1, 'La pluie me rappelle la mer. La bière aussi, en moins salée.'),
  ('ysolde', 2, 'J''ai mené un navire à travers trois tempêtes. Aujourd''hui, je fuis l''averse comme tout le monde.'),
  ('ysolde', 3, 'Mon second disait que je parlais aux nuages. Il avait raison, et ils répondaient.'),
  ('ysolde', 4, 'Quand il pleut, je viens ici. Quand il fait beau, je pense à venir ici.'),
  ('ysolde', 5, 'Prends cette ancre. Une taverne aussi a besoin de quelque chose qui la retienne.'),
  ('pip', 1, 'Soupe ! Chaude ! Beaucoup !'),
  ('pip', 2, 'Pip a goûté la soupe de la ville d''à côté. Pip préfère ta soupe. Pip ne ment pas, sauf le mardi.'),
  ('pip', 3, 'Les gens ont peur de Pip. Toi, tu donnes la soupe. Tu es bizarre. Pip aime bien.'),
  ('pip', 4, 'Pip a appris à dire « merci ». Merci. Voilà.'),
  ('pip', 5, 'Pour toi, champignon porte-bonheur. Pas le manger. Pip l''a fait une fois.'),
  ('anselme', 1, 'L''abbaye fait un cidre honnête. Le tien l''est un peu moins, et il est bien meilleur.'),
  ('anselme', 2, 'Le père abbé croit que je visite les malades. J''en visite un : moi, quand j''ai soif.'),
  ('anselme', 3, 'Il y a quelque chose de sacré dans une pomme bien pressée.'),
  ('anselme', 4, 'Je prie pour ta taverne chaque soir. Surtout pour le pressoir.'),
  ('anselme', 5, 'Cette pomme vient du verger de l''abbaye. Elle est bénie, ou du moins bien lavée.'),
  ('lune', 1, 'Les étoiles disent que tu vas servir un thé. Elles se trompent rarement.'),
  ('lune', 2, 'Saturne est bien placé ce soir. Ou alors c''est la lanterne.'),
  ('lune', 3, 'J''ai dressé ton thème : ascendant tavernier.'),
  ('lune', 4, 'Les nuits claires, le ciel parle. Ici, ce sont les clients. C''est presque pareil.'),
  ('lune', 5, 'Garde cette étoile de cuivre. Quand tu doutes, regarde-la, puis sers un thé.'),
  ('fennec', 1, 'Je jure que je paie. Regarde, je paie. Avec mon argent, en plus.'),
  ('fennec', 2, 'Avant, je comptais les sorties d''une pièce. Maintenant, je compte les chopes.'),
  ('fennec', 3, 'Ton tiroir-caisse ferme mal, tu sais. Je dis ça pour t''aider.'),
  ('fennec', 4, 'Le guet m''a salué ce matin. Poliment. Je ne savais pas quoi faire de mes mains.'),
  ('fennec', 5, 'Prends mon dernier crochet. Je n''en ai plus besoin : mes amis m''ouvrent la porte.'),
  ('hugues', 1, 'J''ai des cartes de partout, sauf d''ici. Personne ne se perd dans ta taverne.'),
  ('hugues', 2, 'Cette tourte mérite une croix sur toutes les cartes, comme un trésor.'),
  ('hugues', 3, 'Une carte, c''est une promesse de voyage. Une tourte, c''est une promesse de rester.'),
  ('hugues', 4, 'J''ai dessiné ta taverne sur ma dernière carte. Je l''ai faite un peu plus grande.'),
  ('hugues', 5, 'Voici la carte du royaume. Ta taverne est au centre. J''ai un peu triché.')
) as lines (regular_id, chapter, line)
on conflict (regular_id, chapter) do update set friendship = excluded.friendship, line = excluded.line;

create table if not exists public.tavern_regulars (
  player_id uuid not null references public.taverns (player_id) on delete cascade,
  regular_id text not null references public.regulars (id),
  friendship integer not null default 0 check (friendship >= 0),
  chapter integer not null default 0 check (chapter between 0 and 5),
  visits integer not null default 0 check (visits >= 0),
  last_visit timestamptz not null default now(),
  primary key (player_id, regular_id)
);

alter table public.regulars enable row level security;
alter table public.regular_chapters enable row level security;
alter table public.tavern_regulars enable row level security;
revoke all on public.regulars, public.regular_chapters, public.tavern_regulars from anon, authenticated;

alter table public.goal_kinds drop constraint if exists goal_kinds_measure_check;
alter table public.goal_kinds add constraint goal_kinds_measure_check check (measure in ('served', 'perfect', 'drink', 'regular'));

insert into public.goal_kinds (id, label, measure, drink, target, reward) values
  ('habitue-1', 'Servir un habitué', 'regular', null, 1, 80),
  ('habitue-4', 'Servir 4 habitués', 'regular', null, 4, 160)
on conflict (id) do update set label = excluded.label, measure = excluded.measure, target = excluded.target, reward = excluded.reward;
