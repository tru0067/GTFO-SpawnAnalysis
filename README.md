# Spawn Analysis

See `thunderstore_package/README.md` for info on using the mod itself.

## Contributing

Change the target r2modman profile in `SpawnAnalysis/SpawnAnalysis.csproj` to
your desired development profile.

In order to prevent git from trying to include this change you can run:
```sh
git update-index --assume-unchanged SpawnAnalysis/SpawnAnalysis.csproj
```

Note that this will mean that any actual legitimate changes to this file won't
be noticed, you'll have to add those manually.

Also, don't forget to change the *Active solution configutation* to *Release*
in *Build > Configuration Manager*.
