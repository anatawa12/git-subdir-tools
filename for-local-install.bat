
dotnet pack -c release git-subdir-copy/git-subdir-copy.csproj
dotnet pack -c release git-subdir-back/git-subdir-back.csproj

dotnet tool uninstall git-subdir-copy -g
dotnet tool uninstall git-subdir-back -g

dotnet tool install --global --add-source ./git-subdir-copy/bin/release git-subdir-copy
dotnet tool install --global --add-source ./git-subdir-back/bin/release git-subdir-back

