# docker buildx bake -f docker-bake.hcl
# IMAGE_NAME=myregistry/reac-site IMAGE_TAG=1.0 docker buildx bake

group "default" {
  targets = ["site"]
}

variable "CI_REGISTRY_IMAGE" {
  default = "reg.home.local/vibecoding/reac-vc"
}

target "site" {
  context    = "."
  dockerfile = "Dockerfile.site"
  tags       = ["${IMAGE_NAME}:${IMAGE_TAG}"]
  labels = {
    "org.opencontainers.image.title"       = "REaC static knowledge base"
    "org.opencontainers.image.description" = "HTML export from reac export-html, served by nginx"
  }
}
