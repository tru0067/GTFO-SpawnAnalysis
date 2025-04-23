# Spawn Analysis

Outputs some info to the console regarding available spawns/spawn probabilities
whenever an enemy wave spawns.

NOTE: By default the analyser samples 10,000 points when building up spawn
probabilities, this isn't a crazy strain on your PC but you probably won't want
it doing that while playing the game, so best to only use this while testing.

## Reading the output

It will always display the spawn probabilities for every kind of wave spawn.
Note that this is the chance that a given wave/group spawns in the given
location *when randomization is triggered*. Waves can trigger this
randomization more or less often depending on their wave settings.

Specifically for `InRelationToClosestAlivePlayer` (regular 'from players'
alarms) it will output some additional parameters. See
<https://gtfo.wiki.gg/wiki/Enemy_Spawning> for some info on how to use this
info.
