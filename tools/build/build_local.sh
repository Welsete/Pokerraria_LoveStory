#!/usr/bin/env bash
# Compila o mod numa máquina Linux (o shell do Cowork no PC do Welsete) e, se a pasta Mods do tModLoader
# estiver montada, copia os .tmod para lá. É o mesmo processo do CI (JamzOJamz/build-tmodloader-mod).
#
# Uso:  bash tools/build/build_local.sh [pasta Mods de destino]
# Primeira vez: instala .NET 8 e 10 e baixa o tModLoader em $HOME/bt (uns 600 MB, menos de 1 minuto).
set -euo pipefail
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
BT="$HOME/bt"
TML_TAG="${TML_TAG:-v2026.09.3.1}"
DEST="${1:-}"
mkdir -p "$BT" && cd "$BT"

export DOTNET_ROOT="$HOME/dotnet" PATH="$HOME/dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
	curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
	bash dotnet-install.sh --channel 8.0 --install-dir "$HOME/dotnet" > /dev/null
	bash dotnet-install.sh --channel 10.0 --install-dir "$HOME/dotnet" > /dev/null
fi
if [ ! -f tml/tMLMod.targets ]; then
	mkdir -p tml && cd tml
	curl -sSL -o t.zip "https://github.com/tModLoader/tModLoader/releases/download/$TML_TAG/tModLoader.zip"
	unzip -q t.zip && rm t.zip
	# Deixa as libs nativas (FNA3D etc.) visíveis no Linux
	sed -i 's|<Exec Command="dotnet |<Exec Command="LD_LIBRARY_PATH=\&quot;$(tMLLibraryPath)/Native/Linux\&quot; dotnet |' tMLMod.targets
	cd "$BT"
fi
mkdir -p ModSources
cat > ModSources/tModLoader.targets <<XML
<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$BT/tml/tMLMod.targets" />
</Project>
XML

export ExtraBuildModFlags="-tmlsavedirectory $BT/tmlsave"
for mod in TmonRuntimes Terramon; do
	# O tModLoader dá ao mod o nome da pasta do .csproj, então o repo vai para ModSources/<mod>
	rsync -a --delete --exclude .git --exclude art --exclude obj --exclude bin "$REPO/" "ModSources/$mod/"
	dotnet build "ModSources/$mod/$mod/$mod.csproj" -c Release --nologo -clp:ErrorsOnly
done
ls -la tmlsave/Mods/*.tmod

if [ -n "$DEST" ]; then
	cp tmlsave/Mods/Terramon.tmod tmlsave/Mods/TmonRuntimes.tmod "$DEST/"
	echo "Copiado para $DEST"
fi
