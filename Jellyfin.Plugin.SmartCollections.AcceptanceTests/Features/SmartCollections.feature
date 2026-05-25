Feature: Smart Collections
    As a Jellyfin administrator
    I want media to be automatically organized into collections based on tags
    So that users can discover related content easily

    Scenario: Tags create matching collections
        Given a tag-title pair with tag "christmas"
        And movies in the library tagged "christmas"
        When the smart collection sync runs
        Then a collection named "Christmas Smart Collection" should exist
        And it should contain the tagged movies

    Scenario: AND matching mode requires all tags
        Given a tag-title pair with tags "action,comedy" in AND mode
        And a movie tagged with both "action" and "comedy"
        And a movie tagged with only "action"
        When the smart collection sync runs
        Then the collection should contain only the movie with both tags

    Scenario: OR matching mode requires any tag
        Given a tag-title pair with tags "action,comedy" in OR mode
        And a movie tagged with only "action"
        And a movie tagged with only "comedy"
        When the smart collection sync runs
        Then the collection should contain both movies

    Scenario: Collection items are removed when tag is removed
        Given a collection with existing items
        And one item no longer matches the tag criteria
        When the smart collection sync runs
        Then the non-matching item should be removed from the collection

    Scenario: Collection image is set from person
        Given a tag-title pair for a person "Tom Hanks"
        And the person has a primary image
        When a new collection is created for that person
        Then the collection image should be set from the person
