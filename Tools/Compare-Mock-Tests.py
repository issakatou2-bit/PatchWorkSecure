"""比較の出典訂正で、保存済みのゲーム画面を変えないことを確認する。"""
import importlib.util
import json
import os
import tempfile
import unittest
from PIL import Image

spec = importlib.util.spec_from_file_location('compare_mock', os.path.join(os.path.dirname(__file__), 'Compare-Mock.py'))
compare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compare)


class ComparisonLabels(unittest.TestCase):
    def test_relabel_keeps_both_screens_and_original(self):
        with tempfile.TemporaryDirectory(prefix='pws-compare-test-') as folder:
            out = os.path.join(folder, 'comparison.png')
            image = Image.new('RGB', (3230, 950), (100, 120, 150))
            compare.heading(image, 'mock')
            image.save(out)
            pixels = image.crop((0, 50, 3230, 950)).tobytes()
            old_hash = compare.digest(out)
            with open(out + '.json', 'w', encoding='utf-8') as target:
                json.dump({'mock_image_sha256': 'original-source'}, target)
            compare.relabel_existing(out, 'unity')
            with Image.open(out) as after:
                self.assertEqual(pixels, after.crop((0, 50, 3230, 950)).tobytes())
            self.assertEqual(old_hash, compare.digest(out + '.before-relabel.png'))
            with open(out + '.json', encoding='utf-8') as source:
                record = json.load(source)
            self.assertEqual('unity', record['reference_kind'])
            self.assertEqual('original-source', record['mock_image_sha256'])
            self.assertEqual(old_hash, record['relabelled_from_sha256'])
            self.assertIn('Unity撮影', record['reference_label'])

    def test_default_mock_label(self):
        image = Image.new('RGB', (3230, 950), 'white')
        self.assertEqual('モック（手本）', compare.heading(image, 'mock'))


if __name__ == '__main__':
    unittest.main()
