# ca-bakery

`ca-bakery` is a tool based on [Cake Build](https://cakebuild.net/) which allow you to define your CI pipeline in a `recipe.yml` file which is implemented using a custom DSL (Domain Specific Language).

Using the custom DSL you can define:
- **Components:**  *projects part of the CI pipeline*
- **Environment:** *environment variables used by the CI pipeline*
- **Bundlers:** *a bundler is a collection of steps to be executed to create a bundle*
- **Artifacts:** *an artifact is a deployable component and it is built using one or more bundles*

`ca-bakery` can be used also to bundle multple projects in one single deployable artifact. For instance you may be developing a web application composed by a NetCore backend project and an Angular client project as well as this you may want to bundle both applications in a single deployable artifact where the NetCore app would be serving the Angular app to the client.

Using  `ca-bakery`  you need just to make sure projects which are part of the CI pipeline must implement a defined interface and once done you can feed `ca-bakery` with your `recipe.yml` file.

![](images/cupcake.png?raw=true)

Documentation: [ca-bakery.readthedocs.io](http://ca-bakery.readthedocs.io/en/latest/)

#### An example of `recipe.yml`
```yml
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
    steps:
      - import: ./recipe-steps.electron.yml
  - name: webapp-bundler
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
          path: src\public\js
#---------------------------------#
#     artifacts configuration     #
#---------------------------------#
artifacts:
  - name: app-nodejs
    path: app-nodejs
    bundler:
      name: webapp-bundler
      enable_compression: true
  - name: app-electron
    path: app-electron
    bundle:
      name: webapp-bundler
      enable_compression: true

```

<div>Icons made by <a href="http://www.freepik.com" title="Freepik">Freepik</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a> is licensed by <a href="http://creativecommons.org/licenses/by/3.0/" title="Creative Commons BY 3.0" target="_blank">CC 3.0 BY</a></div>
