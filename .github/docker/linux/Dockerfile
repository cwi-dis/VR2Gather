# game-ci's Unity image with cwipc added, for the CI builds in a Linux container
# (VR2Gather#355, copied from cwi-dis/cwipc_unity#71). Built in the CI job and passed to game-ci/unity-builder
# as customImage.
#
# UNITY_VERSION: the project's Unity version (VRTApp-Develop/ProjectSettings/ProjectVersion.txt).
# CWIPC_VERSION: a cwipc release tag, or "nightly". The -built- archive of that release is
# installed with cwipc's get-cwipc.sh, with its runtime dependencies (apt), without Python.

# Image version 4 (game-ci/docker v4.0.0) is Ubuntu 24.04, as cwipc's Linux archive needs;
# version 3 is Ubuntu 22.04. Version 4 images exist only for recent Unity versions
# (for example 6000.3.26f1, not 6000.3.10f1).
ARG UNITY_VERSION
ARG GAMECI_IMAGE_VERSION=4
FROM unityci/editor:ubuntu-${UNITY_VERSION}-linux-il2cpp-${GAMECI_IMAGE_VERSION}

ARG CWIPC_VERSION=v8.3.0

# No sudo and no /etc/udev in this image: cwipc's install-deps-ubuntu2404.sh copes with both
# since cwi-dis/cwipc#383 (cwi-dis/cwipc@94d4567a, in the nightly since 12d6ac32). curl and
# ca-certificates are in the image.
RUN curl -sSfL "https://raw.githubusercontent.com/cwi-dis/cwipc/${CWIPC_VERSION}/scripts/get-cwipc.sh" -o /tmp/get-cwipc.sh \
    && bash /tmp/get-cwipc.sh --method archive --deps --no-python "${CWIPC_VERSION}" /opt/cwipc \
    && rm -f /tmp/get-cwipc.sh \
    && rm -rf /var/lib/apt/lists/*

# Let the dynamic loader find cwipc's libraries without LD_LIBRARY_PATH: game-ci passes only
# a fixed set of environment variables into the container.
RUN echo /opt/cwipc/lib > /etc/ld.so.conf.d/cwipc.conf && ldconfig
