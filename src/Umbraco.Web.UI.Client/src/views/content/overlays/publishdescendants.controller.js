(function () {
    "use strict";

    function PublishDescendantsController($scope, localizationService) {

        var vm = this;

        function onInit() {

            vm.includeUnpublished = false;
            vm.variants = $scope.model.variants;
            vm.labels = {};

            if (!$scope.model.title) {
                localizationService.localize("buttons_publishDescendants").then(function (value) {
                    $scope.model.title = value;
                });
            }

            if (vm.variants.length > 1) {

                //now sort it so that the current one is at the top
                vm.variants = _.sortBy(vm.variants, function (v) {
                    return v.active ? 0 : 1;
                });

                var active = _.find(vm.variants, function (v) {
                    return v.active;
                });

                if (active) {
                    //ensure that the current one is selected
                    active.publishDescendants = true;
                    active.save = true;
                }
                
            } else {
                // localize help text for invariant content
                vm.labels.help = {
                    "key": "content_publishDescendantsHelp",
                    "tokens": []
                };
                // add the node name as a token so it will show up in the translated text
                vm.labels.help.tokens.push(vm.variants[0].name);
            }
            
        }

        //when this dialog is closed, reset all 'publish' flags
        $scope.$on('$destroy', function () {
            for (var i = 0; i < vm.variants.length; i++) {
                vm.variants[i].publishDescendants = false;
                vm.variants[i].save = false;
            }
        });

        onInit();

    }

    angular.module("umbraco").controller("Umbraco.Overlays.PublishDescendantsController", PublishDescendantsController);

})();
