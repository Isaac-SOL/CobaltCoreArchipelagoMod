# Chapter 03 - Artifacts

## Overview

Artifacts are primarily defined by their name, their description, and what pool they belong to.

## Artifact Hooks

Artifacts have a multitude of things they can easily influence. All of these can be found by exploring the methods on the Artifact class. 

## Effects that aren't Hooks

Some effects are not easily created as a hook. Instead, they would be easier to achieve by checking if an artifact is in the player's possession, and applying logic from there.

## Supplementary Reading

These artifacts utilize hooks:
* [BuriedKnowledge.cs](./../Artifacts/BuriedKnowledge.cs)

These artifacts do not utilize hooks, and are instead checked elsewhere:
* [Lexicon.cs](./../Artifacts/Lexicon.cs)