# Git Workflow — HorseProject Unity

Guide for two developers working together on the project without stepping on each other's toes.

---

## Golden Rule

> **Never work directly on `main`.  
> Always create a branch for each task.**

---

## Installation (once only)

### 1. Install Git for Windows
Download and install Git: https://git-scm.com/download/win  
Leave all options as default.

### 2. Configure your Git identity
Open PowerShell and type:

```powershell
git config --global user.name "Your Name"
git config --global user.email "your@email.com"
```

### 3. Clone the project
```powershell
cd C:\Users\YourName\Documents
git clone https://github.com/khrabehi/HorseProject.git
cd HorseProject
```

### 4. Delete Library/ so Unity regenerates the packages
```powershell
Remove-Item -Recurse -Force Library/
```
Then open Unity and wait for all packages to download (5–10 min).

---

## Issues — Track your work before coding

**An Issue is a task card on GitHub.** Create one before starting any piece of work — it gives the team visibility on what everyone is doing and provides a reference to link to your branch and Pull Request.

### Creating an Issue

1. Go to https://github.com/khrabehi/HorseProject/issues
2. Click **"New issue"**
3. Fill in:
   - **Title** — short and clear: `Add idle breathing animation to horse`
   - **Description** — what needs to be done and why
   - **Labels** — use labels to categorise (see below)
   - **Assignee** — assign it to yourself if you're taking it

### Recommended labels

| Label | When to use |
|-------|-------------|
| `feature` | New functionality |
| `bug` | Something broken |
| `animation` | Blender / Animator work |
| `scene` | Unity scene changes |
| `documentation` | Docs, README, guides |
| `in progress` | Currently being worked on |

### Issue example

```
Title: Add idle_breathe animation to horse

Description:
The horse is currently static. We need a looping breathing animation
to make it feel alive when no interaction is happening.

Tasks:
- [ ] Create animation in Blender
- [ ] Export FBX and import into Unity
- [ ] Connect clip to Animator Controller (idle state)
- [ ] Test on Quest

Labels: feature, animation
Assignee: Khalis
```

Each Issue gets a number automatically (e.g. `#4`). **Remember this number** — you'll use it to link your branch and Pull Request.

---

## Daily Workflow

### Before starting work

Always pull the latest changes from the team first:

```powershell
git checkout main
git pull origin main
```

### Create a branch for your task

Name your branch clearly and **include the Issue number**:

```powershell
# Format: type/issue-number-short-description
git checkout -b feature/4-idle-breathing-animation
git checkout -b feature/7-horse-state-machine
git checkout -b fix/12-missing-xray-prefab
git checkout -b anim/9-stress-recoil-blender
```

Linking the Issue number in the branch name makes it immediately clear what the branch is for.

### Work and save

After making changes in Unity, **close Unity before committing** to avoid conflicts on open files. Then:

```powershell
# See what changed
git status

# Add modified files
git add .

# Create a commit with a clear message
git commit -m "feat(#4): add idle_breathe animation loop on horse"
```

### Commit message format

```
type(#issue): short description
```

| Prefix | When to use |
|--------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `anim` | Animation added or modified |
| `scene` | Unity scene modified |
| `refactor` | Code reorganisation, no behaviour change |
| `docs` | Documentation update |
| `wip` | Work in progress (temporary save) |

Examples:
```
feat(#4): add idle_breathe and idle_tail_swish animations
anim(#9): create stress_recoil animation in Blender
fix(#12): restore missing MainHorse prefab reference
scene(#6): update HandsDemoScene with new horse states
docs(#1): add Git workflow guide for the team
```

### Push your branch to GitHub

```powershell
git push origin feature/4-idle-breathing-animation
```

---

## Pull Requests — Merge your work into main

**Never run `git merge` directly in your local terminal.**  
Always go through a Pull Request on GitHub so your teammate can review.

### Creating a Pull Request

1. Go to https://github.com/khrabehi/HorseProject
2. GitHub shows a **"Compare & pull request"** button automatically after your push — click it
3. Fill in the Pull Request:

**Title:** `feat(#4): add idle breathing animation to horse`

**Description template:**
```
## What does this PR do?
Adds the idle_breathe looping animation to the horse Animator Controller.

## Related Issue
Closes #4

## Changes made
- Created idle_breathe animation in Blender (2s loop)
- Exported FBX and imported into Unity
- Connected clip to Animator Controller idle layer
- Set stressLevel = 0 as entry condition

## How to test
1. Open HandsDemoScene
2. Press Play
3. The horse should visibly breathe while idle

## Screenshots / Videos
(add if relevant)
```

4. Set **Assignees** (yourself) and **Reviewers** (your teammate)
5. Click **"Create pull request"**

### The magic keywords — auto-close Issues

Use these words in your PR description to **automatically close the linked Issue** when the PR is merged:

```
Closes #4
Fixes #12
Resolves #7
```

When the PR is merged into `main`, GitHub will automatically mark Issue #4 as closed. No manual action needed.

### After the PR is merged

- Click **"Delete branch"** on GitHub to keep the repo clean
- Pull the updated `main` locally:

```powershell
git checkout main
git pull origin main
```

---

## Getting your teammate's work

After they merge their Pull Request into `main`:

```powershell
git checkout main
git pull origin main
```

If you were on a branch in progress:

```powershell
# Save your work first
git add .
git commit -m "wip: save before sync"

# Get latest main
git checkout main
git pull origin main

# Go back to your branch and integrate main's changes
git checkout feature/your-branch
git merge main
```

---

## Managing Unity Conflicts

Conflicts happen most often on `.unity` (scene) and `.prefab` files.  
**The best way to avoid them:**

- **Never work on the same scene at the same time**
- Communicate before opening a scene: *"I'm working on HandsDemoScene"*
- Commit often (several times per session)
- When you're done with a scene, commit and push before switching to something else

If a conflict happens anyway:

```powershell
# Git signals a conflict
git status
# You'll see: "both modified: Assets/Scenes/MyScene.unity"

# Simplest option: keep your version
git checkout --ours Assets/Scenes/MyScene.unity
git add Assets/Scenes/MyScene.unity
git commit -m "fix: resolve scene conflict, kept local version"

# Or keep your teammate's version
git checkout --theirs Assets/Scenes/MyScene.unity
```

---

## Full Workflow Diagram

```
GitHub Issues (task planning)
        │
        │  #4 Add idle breathing animation
        │
        ▼
main (stable, always working)
 │
 ├── feature/4-idle-breathing-animation    ← Khalis works here
 │       └── commits: feat(#4): ...
 │       └── Pull Request → "Closes #4" → merge into main
 │       └── Issue #4 auto-closed ✓
 │
 └── feature/7-horse-state-machine         ← Ezra works here
         └── commits: feat(#7): ...
         └── Pull Request → "Closes #7" → merge into main
         └── Issue #7 auto-closed ✓
```

---

## Useful Commands

```powershell
# See the status of your files
git status

# See commit history
git log --oneline

# See all branches
git branch -a

# Switch branch
git checkout branch-name

# Undo uncommitted changes on a file
git restore Assets/MyFile.cs

# Undo the last commit (keeps the changes)
git reset --soft HEAD~1
```

---

## Troubleshooting

### "I made a mess, I want to go back"

```powershell
# See recent commits
git log --oneline

# Go back to a previous commit (without losing files)
git reset --soft abc1234

# Go back to the last commit state (WARNING: loses uncommitted changes)
git reset --hard HEAD
```

### "I committed directly on main by mistake"

```powershell
# Create a branch from where you are
git checkout -b feature/my-forgotten-branch

# Go back to main and undo the commit
git checkout main
git reset --hard HEAD~1

# Force push the fix
git push origin main --force
```

### "Unity won't compile after a pull"

1. Close Unity
2. Delete `Library/`:
```powershell
Remove-Item -Recurse -Force Library/
```
3. Reopen Unity and wait for reimport

### "My branch is behind main and I'm getting conflicts"

```powershell
git checkout main
git pull origin main
git checkout feature/your-branch
git merge main
# Resolve any conflicts, then:
git add .
git commit -m "merge: sync with latest main"
git push origin feature/your-branch
```

---

*Maintained by the HorseProject team — update as needed based on team feedback.*
