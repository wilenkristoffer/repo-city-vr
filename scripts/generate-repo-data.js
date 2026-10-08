import { execSync } from 'node:child_process'
import { statSync, mkdirSync, writeFileSync, readFileSync } from 'node:fs'
import path from 'node:path'

const root = process.cwd()

const trackedFiles = execSync('git ls-files', { cwd: root, encoding: 'utf8' })
  .split('\n')
  .map((line) => line.trim())
  .filter(Boolean)

// Single git log call for all history, parsed once, rather than one call per file.
// %x00 is git's own format escape for a literal NUL byte — written here as text so
// execSync doesn't choke on an actual NUL inside the command string.
const log = execSync('git log --pretty=format:%x00%cI%x00 --name-only', {
  cwd: root,
  encoding: 'utf8',
  maxBuffer: 1024 * 1024 * 64,
})

const history = new Map() // path -> { commits, lastModified }
const blocks = log.split('\x00').filter((chunk) => chunk !== '')
// blocks alternate: [date, filesBlock, date, filesBlock, ...] in newest-first order
for (let i = 0; i < blocks.length - 1; i += 2) {
  const date = blocks[i].trim()
  const filesBlock = blocks[i + 1]
  const filePaths = filesBlock.split('\n').map((p) => p.trim()).filter(Boolean)
  for (const filePath of filePaths) {
    const entry = history.get(filePath)
    if (entry) {
      entry.commits += 1
      // log is newest-first, so the first time we see a path its lastModified is already set
    } else {
      history.set(filePath, { commits: 1, lastModified: date })
    }
  }
}

const dirSet = new Set([''])
const files = trackedFiles.map((filePath) => {
  const dir = path.dirname(filePath)
  const normalizedDir = dir === '.' ? '' : dir.replace(/\\/g, '/')
  const name = path.basename(filePath)
  // path.extname treats a leading-dot-only name (".gitignore") as having no extension
  const rawExt = path.extname(name)
  const ext = rawExt ? rawExt.slice(1).toLowerCase() : ''
  const size = statSync(path.join(root, filePath)).size
  const hist = history.get(filePath)

  let prefix = normalizedDir
  while (true) {
    dirSet.add(prefix)
    if (prefix === '') break
    const parent = prefix.includes('/') ? prefix.slice(0, prefix.lastIndexOf('/')) : ''
    prefix = parent
  }

  return {
    path: filePath,
    dir: normalizedDir,
    name,
    ext,
    size,
    commits: hist ? hist.commits : 0,
    lastModified: hist ? hist.lastModified : null,
  }
})

const dirs = [...dirSet].sort()

let repoName = 'repo-city-unity'
try {
  const pkg = JSON.parse(readFileSync(path.join(root, 'package.json'), 'utf8'))
  if (pkg.name) repoName = pkg.name
} catch {
  // no package.json readable, keep fallback
}

const result = { repoName, files, dirs }

const publicDir = path.join(root, 'Assets', 'Resources', 'RepoCity')
mkdirSync(publicDir, { recursive: true })
writeFileSync(path.join(publicDir, 'repo-data.json'), JSON.stringify(result, null, 2))

console.log(`Wrote Assets/Resources/RepoCity/repo-data.json: ${files.length} files, ${dirs.length} dirs`)
