PROJECT   := src/Challenges/Challenges.csproj
SDK_IMAGE := mcr.microsoft.com/dotnet/sdk:10.0
UID       := $(shell id -u)
GID       := $(shell id -g)
DEBUG_OUTPUT := $(shell sed -n 's/.*<OutputPath>\(.*\)<\/OutputPath>.*/\1/p' $(PROJECT))

# Run the SDK as the host user so files written to bind mounts are not root-owned.
DOCKER_RUN = docker run --rm \
	--user $(UID):$(GID) \
	-e HOME=/tmp \
	-e DOTNET_CLI_HOME=/tmp \
	-e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
	-v "$(CURDIR)":/src \
	-v "$(HOME)/.nuget":/tmp/.nuget \
	-v "$(DEBUG_OUTPUT)":"$(DEBUG_OUTPUT)" \
	-w /src \
	$(SDK_IMAGE)

.DEFAULT_GOAL := help

.PHONY: help debug release restore clean dirs panorama catalog

help: ## Show this help
	@echo "CS2 Challenges"
	@echo ""
	@echo "Usage: make <target>"
	@echo ""
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

dirs:
	@mkdir -p "$(HOME)/.nuget" "$(DEBUG_OUTPUT)"

catalog: ## Generate builder/catalog.json from extractors
	python3 tools/generate_catalog.py

debug: dirs catalog ## Build Debug (development game server)
	$(DOCKER_RUN) dotnet build $(PROJECT) -c Debug

release: dirs catalog ## Publish Release (production game server)
	$(DOCKER_RUN) dotnet publish $(PROJECT) -c Release

restore: dirs ## Restore NuGet packages
	$(DOCKER_RUN) dotnet restore $(PROJECT)

clean: dirs ## Remove build artifacts
	$(DOCKER_RUN) dotnet clean $(PROJECT)

panorama: ## Validate HUD Panorama XML and CSS
	python3 tools/validate_panorama.py
