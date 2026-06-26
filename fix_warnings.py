import subprocess

dst = r'd:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\Services\DbService.cs'

result = subprocess.run(
    ['git', '-C', 'd:\\BaiduSyncdisk\\宇程科技智能家居\\编程\\宇辰信息中心',
     'show', 'HEAD:FactoryProductManager/Services/DbService.cs'],
    capture_output=True
)
content = result.stdout.decode('utf-8')

content_lines = content.splitlines(keepends=True)
new_lines = []

for l in content_lines:
    if ' ?? ' in l:
        new_lines.append(l)
        continue

    def find_kth_paren(s, start, k):
        pos = start
        count = 0
        while pos < len(s):
            if s[pos] == ')':
                count += 1
                if count == k:
                    return pos
            pos += 1
        return -1

    modified = False

    # === TERNARY PATTERNS ===
    def process_ternary_string(l, pattern, fallback):
        idx = l.find(pattern)
        if idx < 0:
            return l, False
        colon = l.find(': ', idx)
        gp = l.find('GetValue(', colon)
        if gp < 0:
            return l, False
        start = gp + len('GetValue(')
        if l[start:start+len('reader.GetOrdinal(')] == 'reader.GetOrdinal(':
            p1 = find_kth_paren(l, start, 1)
            p2 = find_kth_paren(l, p1+1, 1)
            gp_close = p2
        else:
            gp_close = find_kth_paren(l, start, 1)
        return l[:gp_close+1] + fallback + l[gp_close+1:], True

    l, done = process_ternary_string(l, '? string.Empty : Convert.ToString(reader.GetValue(', ' ?? string.Empty')
    if done:
        modified = True

    if not modified:
        l, done = process_ternary_string(l, '? string.Empty : Convert.ToString(itemReader.GetValue(', ' ?? string.Empty')
        if done:
            modified = True

    if not modified:
        l, done = process_ternary_string(l, '? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal(', ' ?? string.Empty')
        if done:
            modified = True

    if not modified:
        l, done = process_ternary_string(l, '? DateTime.Now : DateTime.Parse(Convert.ToString(reader.GetValue(', ' ?? DateTime.Now.ToString()')
        if done:
            modified = True

    if not modified:
        l, done = process_ternary_string(l, '? DateTime.Now : DateTime.Parse(Convert.ToString(reader.GetValue(reader.GetOrdinal(', ' ?? DateTime.Now.ToString()')
        if done:
            modified = True

    # === DIRECT PATTERNS (only if NOT modified by ternary) ===
    if not modified:
        if 'Convert.ToString(reader.GetValue(' in l:
            idx = l.rfind('Convert.ToString(reader.GetValue(')
            start = idx + len('Convert.ToString(reader.GetValue(')
            if l[start:start+len('reader.GetOrdinal(')] == 'reader.GetOrdinal(':
                p1 = find_kth_paren(l, start, 1)
                p2 = find_kth_paren(l, p1+1, 1)
                gp_close = p2
            else:
                gp_close = find_kth_paren(l, start, 1)
            l = l[:gp_close+1] + ' ?? string.Empty' + l[gp_close+1:]

        if 'Convert.ToString(itemReader.GetValue(' in l:
            idx = l.rfind('Convert.ToString(itemReader.GetValue(')
            start = idx + len('Convert.ToString(itemReader.GetValue(')
            gp_close = find_kth_paren(l, start, 1)
            l = l[:gp_close+1] + ' ?? string.Empty' + l[gp_close+1:]

    new_lines.append(l)

content = ''.join(new_lines)

with open(dst, 'w', encoding='utf-16') as f:
    f.write(content)

print('Done')
