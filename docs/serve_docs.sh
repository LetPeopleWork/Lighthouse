#!/bin/bash

bundle install
echo "Starting Jekyll with live reload..."
bundle exec jekyll serve --livereload --force_polling --incremental --watch