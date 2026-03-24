// Profanity filter using the `obscenity` library.
// Handles leet speak, Unicode substitutions, character elongation, and embedded words.

import {
  RegExpMatcher,
  englishDataset,
  englishRecommendedTransformers,
} from "https://esm.sh/obscenity@0.4.0";

const matcher = new RegExpMatcher({
  ...englishDataset.build(),
  ...englishRecommendedTransformers,
});

export function containsProfanity(input: string): boolean {
  return matcher.hasMatch(input);
}
