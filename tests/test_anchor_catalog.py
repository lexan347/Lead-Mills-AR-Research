import copy
import json
import unittest
from pathlib import Path

from tools.validate_anchor_catalog import validate_catalog


ROOT = Path(__file__).resolve().parents[1]


class AnchorCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalog = json.loads(
            (ROOT / "config" / "anchors.example.json").read_text(encoding="utf-8")
        )

    def test_example_is_valid(self):
        self.assertEqual(validate_catalog(self.catalog), [])

    def test_partial_coordinate_is_rejected(self):
        candidate = copy.deepcopy(self.catalog)
        candidate["anchors"][0]["latitude"] = 42.0
        errors = validate_catalog(candidate)
        self.assertTrue(any("both be null" in error for error in errors))

    def test_public_unsurveyed_anchor_is_rejected(self):
        candidate = copy.deepcopy(self.catalog)
        candidate["anchors"][0]["publicRelease"] = True
        errors = validate_catalog(candidate)
        self.assertTrue(any("cannot be publicRelease" in error for error in errors))

    def test_duplicate_id_is_rejected(self):
        candidate = copy.deepcopy(self.catalog)
        candidate["anchors"].append(copy.deepcopy(candidate["anchors"][0]))
        errors = validate_catalog(candidate)
        self.assertTrue(any("duplicated" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
