# docker buildx bake -f docker-bake.hcl
# IMAGE_NAME=myregistry/reac-site IMAGE_TAG=1.0 docker buildx bake

group "default" {
  targets = ["site"]
}

variable "CI_REGISTRY_IMAGE" {
  default = "reg.serabass.kz/vibecoding/reac-vc"
}

target "site" {
  context    = "."
  dockerfile = "Dockerfile.site"
  tags       = ["${CI_REGISTRY_IMAGE}:latest"]
  pull       = true
  push       = true
  labels = {
    "org.opencontainers.image.title"       = "REaC static knowledge base"
    "org.opencontainers.image.description" = "HTML export from reac export-html, served by nginx"
  }
}
