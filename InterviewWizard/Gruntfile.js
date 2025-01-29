/// <binding BeforeBuild='uglify' ProjectOpened='watch:tasks, watch' />
/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. https://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),
        clean: {
            js: ['wwwroot/js/*.min.js'],
        },
        uglify: { //minify task
            options: {
                banner: '/*! <%= pkg.name %> <%= grunt.template.today("yyyy-mm-dd") %> */\n',
                mangle: true
            },
            build: {
                files: [{
                    expand: true,
                    cwd: 'wwwroot/js', //source directory
                    src: ['iw-*.js', '!iw-*.min.js'], //source file
                    dest: 'wwwroot/js', //destination directory
                    ext: '.min.js' //destination file extension
                }]
            }
        },
        watch: { //watching these files for changes
            files: ['wwwroot/js/iw-*.js', '!wwwroot/js/*.min.js'],
            tasks: ['clean','uglify']
        }
    });
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-contrib-watch');
};