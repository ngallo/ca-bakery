# ca-bakery

`ca-bakery` is a tool based on [cake build](https://cakebuild.net/) which enables you to define your CI/CD pipeline in a `recipe.yml` file which is implemented using a custom DSL (Domain Specific Language).

The custom DSL is used to define:
- **Components:**  *projects part of the CI pipeline*
- **Environment:** *environment variables used by the CI pipeline*
- **Bundlers:** *a bundler is a collection of steps to be executed in order to create a bundle*
- **Artifacts:** *an artifact is a deployable component and it is built using one or more bundlers*

`ca-bakery` can be used to bundle multiple projects in one single deployable artifact. A common use case would be a web application composed by a NetCore backend project and an Angular client project that need to be bundled into a single deployable artifact where the NetCore app serves to the client the Angular app.

Components to be part of the build process must be defines in the `recipe.yml` file, as well as this they must implement a build script compliant to `ca-bakery`.

The `ca-bakery` pipeline is implemented in a script which can be executed both on the local dev machine and the integration server. This approach makes application based on `ca-bakery` reproducible anywhere.

Here you can find the full documentation: [ca-bakery.readthedocs.io](http://ca-bakery.readthedocs.io/en/latest/)

![](docs/images/cupcake.png?raw=true)

#### `recipe.yml`:
```yaml
#---------------------------------#
#      general configuration      #
#---------------------------------#
# cake build version
version: 1
# name
name: angular-node-electron-seed
# environment variables
environment:
  CAKE_BUILD_VERSION: 001
#---------------------------------#
#      build configuration        #
#---------------------------------#
build:
  dist: dist
  release_notes: RELEASE_NOTES.md
#---------------------------------#
#    components configuration     #
#---------------------------------#
components:
  - name: server-component
    path: server
    build:
      type: npm
      dist: dist
  - name: client-component
    path: client
    build:
      type: npm
      dist: dist
#---------------------------------#
#     bundlers configuration      #
#---------------------------------#
bundlers:
  - name: electron-bundler
    imports:
      - ./recipe-steps.electron.yml
    steps:
  - name: webapp-bundler
    imports:
    steps:
      - operation: copy
        from:
          component: server-component
          context: component
          path: node_modules
        to:
          path: node_modules
      - operation: copy
        from:
          component: server-component
          context: dist
          path:
        to:
          path:
      - operation: copy
        from:
          component: client-component
          context: dist
          path:
          extensions: /**.js/**
        to:
          path: src/public/js
#---------------------------------#
#     artifacts configuration     #
#---------------------------------#
artifacts:
  - name: app-nodejs
    path: app-nodejs
    bundle:
      name: webapp-bundler
      enable_compression: true
  - name: app-electron
    path: app-electron
    bundle:
      name: webapp-bundler
      enable_compression: true
```

<div>Icons made by <a href="http://www.freepik.com" title="Freepik">Freepik</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a> is licensed by <a href="http://creativecommons.org/licenses/by/3.0/" title="Creative Commons BY 3.0" target="_blank">CC 3.0 BY</a></div>
